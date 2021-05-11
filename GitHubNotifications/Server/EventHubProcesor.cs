using Azure.Data.Tables;
using Azure.Messaging.EventHubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;
using GitHubNotifications.Models;
using GitHubNotifications.Shared;
using System.Runtime.CompilerServices;

namespace GitHubNotifications
{
    public class EventHubProcessor : IHostedService, IDisposable
    {
        public const string PR_PK = "pr";
        private readonly ILogger<EventHubProcessor> _logger;
        private readonly IHubContext<NotificationsHub> _hubContext;
        private readonly EventProcessorClient _processor;
        private readonly TableServiceClient tableService;
        private readonly TableClient prTable;
        private readonly TableClient commentTable;
        readonly StringBuilder sbComment = new();

        public EventHubProcessor(
            ILogger<EventHubProcessor> logger,
            IHubContext<NotificationsHub> hub,
            EventProcessorClient processor,
            TableServiceClient tableService)
        {
            _logger = logger;
            _hubContext = hub;
            _processor = processor;
            this.tableService = tableService;
            prTable = tableService.GetTableClient("prs");
            commentTable = tableService.GetTableClient("comments");
            tableService.CreateTableIfNotExists("users");
            processor.ProcessErrorAsync += async (args) =>
            {
                _logger.LogError(args.Exception.ToString());
                await Task.Yield();
            };

            processor.ProcessEventAsync += async (args) =>
            {
                await DoWork(args);
            };
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(EventHubProcessor)} Hosted Service running.");
            await prTable.CreateIfNotExistsAsync(stoppingToken);
            await commentTable.CreateIfNotExistsAsync(stoppingToken);
            await _processor.StartProcessingAsync(stoppingToken);
        }

        private async Task DoWork(object state)
        {
            ProcessEventArgs args = (ProcessEventArgs)state;

            if (args.HasEvent)
            {
                await ProcessEvent(args.Data);
                await args.UpdateCheckpointAsync();
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(EventHubProcessor)} Hosted Service is stopping.");

            return Task.CompletedTask;
        }

        public void Dispose()
        { }

        public async Task ProcessEvent(EventData data)
        {
            string eventType = null;
            var eventBody = data.EventBody.ToString();
            var o = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(eventBody);
            string decoded = string.Empty;
            foreach (var element in o)
            {
                try
                {
                    if (element.Key == "headers")
                    {
                        eventType = element.Value.EnumerateObject().First(v => v.Name == "X-GitHub-Event").Value[0].GetString();
                    }
                    if (element.Key == "content")
                    {
                        decoded = Encoding.UTF8.GetString(Convert.FromBase64String(element.Value.GetString()));
                        Type webhookType = eventType switch
                        {
                            "check_suite" => typeof(CheckSuiteEvent),
                            "pull_request" => typeof(PullRequestEvent),
                            "pull_request_review_comment" => typeof(PullRequestReviewEvent),
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
                            await SendPrCommentMail(r, commentTable);
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

        public async Task SendPrCommentMail(PullRequestReviewEvent pr, TableClient table)
        {
            PRComment inReplyTo = null;
            if (pr.Comment.InReplyToId > 0)
            {
                try
                {
                    inReplyTo = await table.GetEntityAsync<PRComment>(pr.PullRequest.User.Login, pr.Comment.InReplyToId.ToString());
                }
                catch
                {
                    _logger.LogWarning($"\t*Found no reply for {pr.Comment.HtmlUrl}");
                }
            }

            _logger.LogInformation($"{pr.Comment.CreatedAt.ToLocalTime()} PRComment Url: {pr.Comment.HtmlUrl}");

            var model = new CommentModel(
                pr.Comment.Id.ToString(),
                pr.Comment.User.Login,
                pr.Comment.HtmlUrl,
                pr.Comment.UpdatedAt,
                pr.PullRequest.Title,
                pr.Comment.Body,
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

    public static class TestAsyncEnumerableExtensions
    {
        public static async Task<List<T>> ToEnumerableAsync<T>(this IAsyncEnumerable<T> asyncEnumerable)
        {
            List<T> list = new List<T>();
            await foreach (T item in asyncEnumerable)
            {
                list.Add(item);
            }
            return list;
        }
    }
}