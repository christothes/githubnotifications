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
            var actions = new List<TableTransactionAction>();
            string[] selectProps = new[] { "PartitionKey,RowKey" };

            switch (evt.Action)
            {
                case "closed" when evt.PullRequest.Merged:
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
                        // Get all the comments to delete.
                        await foreach (var comment in commentTable.QueryAsync<PRComment>(
                            e => e.PartitionKey == evt.PullRequest.User.Login && e.PrNumber == evt.PullRequest.Number.ToString(),
                            select: selectProps,
                            cancellationToken: _token))
                        {
                            actions.Add(new TableTransactionAction(TableTransactionActionType.Delete, comment));
                        }
                        // Batch delete the comments related to the merged PR.
                        int skip = 0;
                        while (skip < actions.Count)
                        {
                            await commentTable.SubmitTransactionAsync(actions.Skip(skip).Take(100), _token);
                            skip += 100;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                    break;
                case "labeled":
                case "unlabeled":
                    string labels = string.Join(";", evt.PullRequest.Labels.Select(l => l.Name));

                    // update the comment lables for this PR.
                    try
                    {
                        await foreach (var comment in commentTable.QueryAsync<PRComment>(
                            e => e.PartitionKey == evt.PullRequest.User.Login && e.PrNumber == evt.PullRequest.Number.ToString(),
                            select: selectProps,
                            cancellationToken: _token))
                        {
                            comment.Labels = labels;
                            actions.Add(new TableTransactionAction(TableTransactionActionType.UpsertMerge, comment));
                        }
                        await commentTable.SubmitTransactionAsync(actions, _token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                    break;
                default:
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
                    break;
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