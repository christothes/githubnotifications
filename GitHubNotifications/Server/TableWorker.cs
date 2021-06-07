using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using GitHubNotifications.Models;
using System.Linq;

namespace GitHubNotifications.Server
{
    public class TableWorker : Worker, IHostedService
    {
        public const string PR_PK = "pr";
        private readonly TableClient commentTable;
        private readonly TableClient prTable;
        private readonly TableClient labelTable;

        public TableWorker(
            ILogger<TableWorker> logger,
            EventDispatcher dispatcher,
            TableServiceClient tableService) : base(dispatcher, logger)
        {
            commentTable = tableService.GetTableClient("comments");
            prTable = tableService.GetTableClient("prs");
            labelTable = tableService.GetTableClient("labels");

            commentTable.CreateIfNotExists();
            prTable.CreateIfNotExists();
            labelTable.CreateIfNotExists();
        }

        protected internal override async Task PrCommentEventHandler(PullRequestReviewEvent evt)
        {
            await prTable.UpsertEntityAsync(
                new PREntity
                {
                    PartitionKey = PR_PK,
                    RowKey = evt.PullRequest.Head.Sha,
                    Title = evt.PullRequest.Title,
                    Url = evt.PullRequest.HtmlUrl,
                    Author = evt.PullRequest.User.Login,
                    Labels = string.Join(";", evt.PullRequest.Labels.Select(l => l.Name))
                },
                TableUpdateMode.Replace,
                cancellationToken: _token);

            await commentTable.UpsertEntityAsync(new PRComment(evt), TableUpdateMode.Replace, cancellationToken: _token);
        }

        protected internal override async Task PullRequestEventHandler(PullRequestEvent evt)
        {
            if (evt.Action == "closed" && evt.PullRequest.Merged)
            {
                try
                {
                    await prTable.DeleteEntityAsync(PR_PK, evt.PullRequest.Head.Sha, cancellationToken: _token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
                try
                {
                    await foreach (var comment in commentTable.QueryAsync<PRComment>(e => e.PartitionKey == evt.PullRequest.User.Login && e.PrNumber == evt.PullRequest.Number.ToString(), cancellationToken: _token))
                    {
                        await commentTable.DeleteEntityAsync(comment.PartitionKey, comment.RowKey, cancellationToken: _token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }
            }
            else
            {
                await prTable.UpsertEntityAsync(
                    new PREntity
                    {
                        PartitionKey = PR_PK,
                        RowKey = evt.PullRequest.Head.Sha,
                        Title = evt.PullRequest.Title,
                        Url = evt.PullRequest.HtmlUrl,
                        Author = evt.PullRequest.User.Login,
                        Labels = string.Join(";", evt.PullRequest.Labels.Select(l => l.Name))
                    },
                    TableUpdateMode.Replace,
                    cancellationToken: _token);
            }
        }

        protected internal override async Task IssueEventHandler(IssueEvent evt)
        {
            await commentTable.UpsertEntityAsync(new PRComment(evt), TableUpdateMode.Replace, cancellationToken: _token);
        }

        protected internal override Task CheckSuiteEventHandler(CheckSuiteEvent evt)
        {
            throw new NotImplementedException();
        }
    }
}