using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using GitHubNotifications.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitHubNotifications.Server
{
    public class TableWorker : Worker, IHostedService
    {
        private readonly TableClient commentTable;
        private readonly TableClient prTable;

        public TableWorker(ILogger<TableWorker> logger, EventDispatcher dispatcher, TableServiceClient tableService)
                : base(dispatcher, logger)
        {
            commentTable = tableService.GetTableClient("comments");
            prTable = tableService.GetTableClient("prs");
            commentTable.CreateIfNotExists();
            prTable.CreateIfNotExists();
        }

        protected internal override async Task PullRequestReviewCommentEventHandler(PullRequestReviewCommentEvent evt)
        {
            await commentTable.UpsertEntityAsync(new PRComment(evt), TableUpdateMode.Replace);
            await prTable.UpsertEntityAsync(new PREntity(evt), TableUpdateMode.Replace);
        }

        protected internal override async Task PullRequestEventHandler(PullRequestEvent evt)
        {
            var actions = new List<TableTransactionAction>();
            string[] selectProps = new[] { "PartitionKey,RowKey" };

            // Always update the PR in case there have been any changes.
            try
            {
                await prTable.UpsertEntityAsync(new PREntity(evt), TableUpdateMode.Replace);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            try
            {
                switch (evt.Action)
                {
                    case "closed":
                        // Get all the comments to delete.
                        _logger.LogInformation($"Deleting comments for PR {evt.PullRequest.HtmlUrl}");
                        await Actions.AllCommentsForPR(
                            (c, pr) => new TableTransactionAction(TableTransactionActionType.Delete, c),
                            evt.PullRequest, commentTable);
                        break;
                    case "labeled":
                    case "unlabeled":
                        // update the comment lables for this PR.
                        _logger.LogInformation($"Updating labels on comments for PR {evt.PullRequest.HtmlUrl}");
                        string labels = string.Join(";", evt.PullRequest.Labels.Select(l => l.Name));
                        await Actions.AllCommentsForPR(
                            (c, pr) =>
                            {
                                c.Labels = labels;
                                return new TableTransactionAction(TableTransactionActionType.UpdateMerge, c);
                            }, evt.PullRequest, commentTable);
                        break;
                    case "review_requested":
                        break;

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        protected internal override async Task IssueCommentEventHandler(IssueCommentEvent evt)
        {
            await commentTable.UpsertEntityAsync(new PRComment(evt), TableUpdateMode.Replace);
        }

        protected internal override Task CheckSuiteEventHandler(CheckSuiteEvent evt)
        {
            return Task.CompletedTask;
        }

        protected internal override Task PullRequestReviewEventHandler(PullRequestReviewEvent evt)
        {
            return Task.CompletedTask;
        }

        protected internal override Task IssueEventHandler(IssueEvent evt)
        {
            return Task.CompletedTask;
        }
    }
}