using System;
using System.Net;
using System.Threading.Tasks;
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

        public CommentController(ILogger<CommentController> logger, TableServiceClient tableService)
        {
            this._logger = logger;
            this.tableService = tableService;
        }

        [HttpGet]
        public async Task<string> GetSas()
        {
            var sasBuilder = new TableSasBuilder("comments", TableSasPermissions.Read, DateTime.Now.AddDays(1));
            return sasBuilder.Sign(null);
        }
    }
}