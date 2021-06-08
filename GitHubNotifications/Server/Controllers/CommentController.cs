using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
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
        private readonly EventDispatcher _dispatcher;

        public CommentController(
            ILogger<CommentController> logger,
            TableServiceClient tableService,
            TableSharedKeyCredential cred,
            IHubContext<NotificationsHub> hub,
            EventDispatcher dispatcher)
        {
            this._logger = logger;
            this.tableService = tableService;
            this.cred = cred;
            this._hubContext = hub;
            this._dispatcher = dispatcher;
        }

        [HttpGet]
        public string GetSasUrl()
        {
            var sasBuilder = new TableSasBuilder("comments", TableSasPermissions.Read, DateTime.Now.AddDays(1));
            return $"https://{cred.AccountName}.table.core.windows.net?{sasBuilder.Sign(cred)}";
        }

        [HttpPost]
        public async Task<IActionResult> TestEvent(Dictionary<string, JsonElement> jsonPayload)
        {
            if (User.GetGitHubLogin() != "christothes")
            {
                return Unauthorized();
            }
            await _dispatcher.ProcessEvent(jsonPayload);
            return Ok();
        }
    }
}