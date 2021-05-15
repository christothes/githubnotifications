using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using GitHubNotifications.Shared;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using GitHubNotifications.Models;
using System.Threading.Tasks;
using Azure;
using System.Net;

namespace WebApplication1.Server.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly TableClient userTable;

        public UserController(ILogger<UserController> logger, TableServiceClient tableService)
        {
            this._logger = logger;
            this.userTable = tableService.GetTableClient("users");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetCurrentUser()
        {
            return Ok(User.Identity.IsAuthenticated ? CreateUserInfo(User) : UserInfo.Anonymous);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetOptions()
        {
            var login = User.GetGitHubLogin();
            UserOptions options = null;

            try
            {
                options = await userTable.GetEntityAsync<UserOptions>(login, login);

            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == (int)HttpStatusCode.NotFound)
                {
                    options = new UserOptions { PartitionKey = login, RowKey = login, OnlyMyPRs = true, Labels = "" };
                }
            }

            // write the user back to the table to update the TimeStamp
            if (options != null)
            {
                await userTable.UpsertEntityAsync(options);
            }

            return Ok(options);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SetOptions(UserOptions options)
        {
            if (options == null)
            {
                return ValidationProblem($"{nameof(options)} is null");
            }
            await userTable.UpsertEntityAsync(options);
            return Ok();
        }

        private UserInfo CreateUserInfo(ClaimsPrincipal claimsPrincipal)
        {
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                return UserInfo.Anonymous;
            }

            var userInfo = new UserInfo
            {
                IsAuthenticated = true
            };

            if (claimsPrincipal.Identity is ClaimsIdentity claimsIdentity)
            {
                userInfo.NameClaimType = claimsIdentity.NameClaimType;
                userInfo.RoleClaimType = claimsIdentity.RoleClaimType;
            }
            else
            {
                userInfo.NameClaimType = JwtClaimTypes.Name;
                userInfo.RoleClaimType = JwtClaimTypes.Role;
            }

            if (claimsPrincipal.Claims.Any())
            {
                var claims = new List<ClaimValue>();
                var nameClaims = claimsPrincipal.FindAll(userInfo.NameClaimType);
                foreach (var claim in nameClaims)
                {
                    claims.Add(new ClaimValue(userInfo.NameClaimType, claim.Value));
                }

                foreach (var claim in claimsPrincipal.Claims.Except(nameClaims))
                {
                    claims.Add(new ClaimValue(claim.Type, claim.Value));
                }

                userInfo.Claims = claims;
            }

            return userInfo;
        }
    }
}
