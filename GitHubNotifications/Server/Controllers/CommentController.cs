using System;
using Azure.Data.Tables;
using Azure.Data.Tables.Sas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public CommentController(ILogger<CommentController> logger, TableServiceClient tableService, TableSharedKeyCredential cred)
        {
            this._logger = logger;
            this.tableService = tableService;
            this.cred = cred;
        }

        [HttpGet]
        public string GetSasUrl()
        {
            var sasBuilder = new TableSasBuilder("comments", TableSasPermissions.Read, DateTime.Now.AddDays(1));
            return $"https://{cred.AccountName}.table.core.windows.net?{sasBuilder.Sign(cred)}";
        }
    }
}