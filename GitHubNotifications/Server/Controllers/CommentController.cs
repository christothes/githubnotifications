using System;
using Azure.Data.Tables;
using Azure.Data.Tables.Sas;
using GitHubNotifications.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GitHubNotifications.Server
{
    [Authorize]
    [Route("[controller]/[action]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ILogger<CommentController> _logger;
        private readonly TableServiceClient tableService;
        private readonly TableSharedKeyCredential cred;
        private readonly IHubContext<NotificationsHub> _hubContext;

        public CommentController(ILogger<CommentController> logger, TableServiceClient tableService, TableSharedKeyCredential cred, IHubContext<NotificationsHub> hub)
        {
            this._logger = logger;
            this.tableService = tableService;
            this.cred = cred;
            this._hubContext = hub;
        }

        [HttpGet]
        public string GetSasUrl()
        {
            var sasBuilder = new TableSasBuilder("comments", TableSasPermissions.Read, DateTime.Now.AddDays(1));
            return $"https://{cred.AccountName}.table.core.windows.net?{sasBuilder.Sign(cred)}";
        }

        [HttpGet]
        public IActionResult TestComment()
        {
            var model = new CommentModel(
                "1234",
                "pr.Comment.User.Login",
                "pr.Comment.HtmlUrl",
                DateTime.Now,
                "pr.PullRequest.Title",
                "pr.Comment.Body",
                "0",
                null);

            _hubContext.Clients.All.SendAsync(
               "NewComment", model).GetAwaiter().GetResult();
               return Ok();
        }
    }
}