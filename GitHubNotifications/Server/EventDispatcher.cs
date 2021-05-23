using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using GitHubNotifications.Models;
using GitHubNotifications.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GitHubNotifications
{
    public class EventDispatcher
    {
        public const string PR_PK = "pr";
        private readonly ILogger<EventDispatcher> _logger;
        private readonly IHubContext<NotificationsHub> _hubContext;
        private readonly TableServiceClient tableService;
        private readonly TableClient prTable;
        private readonly TableClient commentTable;

        public EventDispatcher(
            ILogger<EventDispatcher> logger,
            IHubContext<NotificationsHub> hub,
            TableServiceClient tableService)
        {
            _logger = logger;
            _hubContext = hub;
            this.tableService = tableService;
            prTable = tableService.GetTableClient("prs");
            commentTable = tableService.GetTableClient("comments");
            tableService.CreateTableIfNotExists("users");
        }

        public async Task ProcessEvent(string eventBody)
        {
            var elementMap = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(eventBody);
            await ProcessEvent(elementMap);
        }

        public async Task ProcessEvent(Dictionary<string, JsonElement> elementMap)
        {
            string eventType = null;
            string decoded = string.Empty;
            foreach (var element in elementMap)
            {
                try
                {
                    if (element.Key == "headers")
                    {
                        eventType = element.Value.EnumerateObject().First(v => v.Name == "X-GitHub-Event").Value[0].GetString();
                        _logger.LogInformation($"received event {eventType}");
                    }
                    if (element.Key == "content")
                    {
                        decoded = Encoding.UTF8.GetString(Convert.FromBase64String(element.Value.GetString()));
                        Type webhookType = eventType switch
                        {
                            "check_suite" => typeof(CheckSuiteEvent),
                            "pull_request" => typeof(PullRequestEvent),
                            "pull_request_review_comment" => typeof(PullRequestReviewEvent),
                            "issue_comment" => typeof(IssueEvent),
                            _ => null,
                        };
                        if (webhookType == null)
                        {
                            _logger.LogError($"unexpected webhook type\n{eventType}");
                            continue;
                        }
                        var webhookObj = JsonSerializer.Deserialize(decoded, webhookType);
                        if (webhookObj is PullRequestEvent pr)
                        {
                            if (pr.Action == "closed" && pr.PullRequest.Merged)
                            {
                                try
                                {
                                    prTable.DeleteEntity(PR_PK, pr.PullRequest.Head.Sha);
                                }
                                catch
                                { }
                                try
                                {
                                    await foreach (var comment in commentTable.QueryAsync<PRComment>(e => e.PartitionKey == pr.PullRequest.User.Login && e.PrNumber == pr.PullRequest.Number.ToString()))
                                    {
                                        await commentTable.DeleteEntityAsync(comment.PartitionKey, comment.RowKey);
                                    }
                                }
                                catch
                                { }
                            }
                            await prTable.UpsertEntityAsync(
                                new PREntity
                                {
                                    PartitionKey = PR_PK,
                                    RowKey = pr.PullRequest.Head.Sha,
                                    Title = pr.PullRequest.Title,
                                    Url = pr.PullRequest.HtmlUrl,
                                    Author = pr.PullRequest.User.Login,
                                    Labels = string.Join(";", pr.PullRequest.Labels.Select(l => l.Name))
                                },
                                TableUpdateMode.Replace);
                        }
                        if (webhookObj is CheckSuiteEvent ch)
                        {
                            await SendCheckStatusMail(ch);
                        }
                        if (webhookObj is PullRequestReviewEvent r)
                        {
                            await prTable.UpsertEntityAsync(
                                new PREntity
                                {
                                    PartitionKey = PR_PK,
                                    RowKey = r.PullRequest.Head.Sha,
                                    Title = r.PullRequest.Title,
                                    Url = r.PullRequest.HtmlUrl,
                                    Author = r.PullRequest.User.Login,
                                    Labels = string.Join(";", r.PullRequest.Labels.Select(l => l.Name))
                                },
                                TableUpdateMode.Replace);

                            await commentTable.UpsertEntityAsync(new PRComment(r), TableUpdateMode.Replace);
                            await SendPrCommentMail(r.Comment, r.PullRequest.User.Login, r.PullRequest.Title, r.PullRequest.Number.ToString(), commentTable);
                        }
                        if (webhookObj is IssueEvent i)
                        {
                            await commentTable.UpsertEntityAsync(new PRComment(i), TableUpdateMode.Replace);
                            await SendPrCommentMail(i.Comment, i.Issue.User.Login, i.Issue.Title, i.Issue.Number.ToString(), commentTable);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    _logger.LogError(ex.StackTrace);
                    _logger.LogError(decoded);
                }
            }
        }

        public async Task SendPrCommentMail(Comment comment, string prAuthor, string prTitle, string prNumber, TableClient table)
        {
            PRComment inReplyTo = null;
            if (comment.InReplyToId > 0)
            {
                try
                {
                    inReplyTo = await table.GetEntityAsync<PRComment>(prAuthor, comment.InReplyToId.ToString());
                }
                catch
                {
                    _logger.LogWarning($"\t*Found no reply for {comment.HtmlUrl}");
                }
            }

            _logger.LogInformation($"{comment.CreatedAt.ToLocalTime()} PRComment Url: {comment.HtmlUrl}");

            var model = new CommentModel(
                comment.Id.ToString(),
                comment.User.Login,
                comment.HtmlUrl,
                comment.UpdatedAt,
                prTitle,
                prNumber,
                prAuthor,
                comment.Body,
                inReplyTo?.RowKey,
                inReplyTo?.Author);

            await _hubContext.Clients.All.SendAsync(
                "NewComment", model);
        }

        public async Task SendCheckStatusMail(CheckSuiteEvent webhookEvent)
        {
            if (webhookEvent.CheckSuite.Conclusion != "failure")
            {
                return;
            }
            PREntity prDetails;
            try
            {
                prDetails = await prTable.GetEntityAsync<PREntity>(PR_PK, webhookEvent.CheckSuite.HeadCommit.Id);
            }
            catch
            {
                _logger.LogWarning("*** Could not find PR in cache ***");
                return;
            }
            var subject = $"Checks {webhookEvent.CheckSuite.Conclusion} for PR: {prDetails.Title}";
            var plainTextContent = $"PR: {prDetails.Url}";

            _logger.LogInformation(subject);
            _logger.LogInformation(plainTextContent);

            await _hubContext.Clients.All.SendAsync(
                "CheckStatus",
                webhookEvent.CheckSuite.UpdatedAt.ToLocalTime(),
                webhookEvent.CheckSuite.Id.ToString(),
                subject,
                plainTextContent,
                prDetails.Url,
                prDetails.Author);
        }
    }
}