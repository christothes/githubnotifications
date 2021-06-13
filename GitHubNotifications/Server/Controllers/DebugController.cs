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
    public class DebugController : ControllerBase
    {
        private readonly ILogger<CommentController> _logger;
        private readonly HubWorker _hubWorker;

        public DebugController(
            ILogger<CommentController> logger,
            HubWorker hubWorker)
        {
            this._logger = logger;
            this._hubWorker = hubWorker;
        }

        [HttpPost]
        public async Task<IActionResult> TestEvent(TestEvent testEvent)
        {
            if (User.GetGitHubLogin() != "christothes")
            {
                return Unauthorized();
            }

            switch (testEvent.EventType)
            {
                case "comment":
                    await _hubWorker.PullRequestReviewCommentEventHandler(TestData.prCommentEvent);
                    break;
                case "check":
                    await _hubWorker.CheckSuiteEventHandler(TestData.checkSuiteFailedEvent);
                    break;
            }
            return Ok();
        }
    }
}