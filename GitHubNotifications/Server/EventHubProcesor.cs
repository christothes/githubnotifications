﻿using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
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

namespace GitHubNotifications.Server.Controllers
{
    public class EventHubProcessor : IHostedService, IDisposable
    {
        private int executionCount = 0;
        public const string PR_PK = "pr";
        private readonly ILogger<EventHubProcessor> _logger;
        private readonly IHubContext<NotificationsHub> _hubContext;
        private readonly EventHubConsumerClient consumer;
        private readonly TableServiceClient tableService;
        private TableClient prTable;

        public EventHubProcessor(
            ILogger<EventHubProcessor> logger,
            IHubContext<NotificationsHub> hub,
            EventHubConsumerClient consumer,
            TableServiceClient tableService)
        {
            _logger = logger;
            _hubContext = hub;
            this.consumer = consumer;
            this.tableService = tableService;
            prTable = tableService.GetTableClient("prs");
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");
            await prTable.CreateIfNotExistsAsync();
            await DoWork(default);
        }

        private async Task DoWork(object state)
        {
            /*
            var t = Task.Run(async () =>
             {
                 while (executionCount < 10)
                 {
                     await _hubContext.Clients.All.SendAsync("ReceiveMessage", "test", executionCount.ToString());
                     await Task.Delay(5000);
                     executionCount++;
                 }
             });
            */

            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(TimeSpan.FromSeconds(45));
            var opts = new ReadEventOptions();
            opts.MaximumWaitTime = TimeSpan.FromSeconds(30);
            var t = Task.Run(async () =>
            {
                await foreach (PartitionEvent receivedEvent in consumer.ReadEventsAsync(opts))
                {
                    await ProcessEvent(receivedEvent.Data);
                }
            });
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        public async Task ProcessEvent(EventData data)
        {
            string eventType = null;
            var eventBody = data.EventBody.ToString();
            var o = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(eventBody);
            foreach (var element in o)
            {
                try
                {
                    //Console.WriteLine(element.Key);
                    if (element.Key == "headers")
                    {
                        eventType = element.Value.EnumerateObject().First(v => v.Name == "X-GitHub-Event").Value[0].GetString();
                    }
                    if (element.Key == "content")
                    {
                        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(element.Value.GetString()));
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
                                catch { }
                                try
                                {
                                    tableService.DeleteTable(GetPRCommentTableName(pr.PullRequest));
                                }
                                catch { }
                            }
                            await prTable.UpsertEntityAsync<PREntity>(new PREntity { PartitionKey = PR_PK, RowKey = pr.PullRequest.Head.Sha, Title = pr.PullRequest.Title, Url = pr.PullRequest.HtmlUrl });
                        }
                        if (webhookObj is CheckSuiteEvent ch)
                        {
                            await SendCheckStatusMail(ch);
                        }
                        if (webhookObj is PullRequestReviewEvent r)
                        {
                            await prTable.UpsertEntityAsync<PREntity>(new PREntity { PartitionKey = PR_PK, RowKey = r.PullRequest.Head.Sha, Title = r.PullRequest.Title, Url = r.PullRequest.HtmlUrl });
                            var commentTable = tableService.GetTableClient(GetPRCommentTableName(r.PullRequest));
                            await commentTable.CreateIfNotExistsAsync();
                            await commentTable.UpsertEntityAsync<PRComment>(new PRComment(r.Comment));

                            //await SendPrCommentMail(r, commentTable);
                        }
                        //webhookObj.Dump();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
        }

        public static async Task SendPrCommentMail(PullRequestReviewEvent pr, TableClient table)
        {
            sbComment.Clear();
            if (pr.Comment.InReplyToId > 0)
            {
                try
                {
                    PRComment inReplyTo = await table.GetEntityAsync<PRComment>("comment", pr.Comment.InReplyToId.ToString());
                    sbComment.AppendLine($"> @{inReplyTo.Author}: {inReplyTo.Body}");
                    sbComment.AppendLine();
                }
                catch
                {
                    Console.WriteLine($"\t*Found no reply for {pr.Comment.HtmlUrl}");
                }
            }

            sbComment.AppendLine($"@{pr.Comment.User.Login}: {pr.Comment.Body}");
            var body = sbComment.ToString();
            var from = new EmailAddress("gh-notif@microsoft.com", "GitHub Notifications");
            var subject = $"Comment ({pr.Comment.User.Login}) : {pr.PullRequest.Title}";
            var to = new EmailAddress("chriss@microsoft.com", "christothes");
            var plainTextContent = body;
            var htmlContent = body;
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            Console.WriteLine(pr.Comment.CreatedAt.ToLocalTime().ToString());
            Console.WriteLine(subject);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine(body);
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
                //(!PRCache.TryGetValue(webhookEvent.CheckSuite.HeadCommit.Id, out prDetails))
                prDetails = await prTable.GetEntityAsync<PREntity>(PR_PK, webhookEvent.CheckSuite.HeadCommit.Id);
            }
            catch
            {
                Console.WriteLine("*** Could not find PR in cache ***");
                return;
            }
            var subject = $"Checks {webhookEvent.CheckSuite.Conclusion} for PR: { prDetails.Title}";
            var plainTextContent = $"PR: {prDetails.Url}";
            var htmlContent = $"<strong>PR: </strong> <a href=\"{prDetails.Url}\">{prDetails.Title}</a>";

            _logger.LogInformation(subject);
            _logger.LogInformation(plainTextContent);

            await _hubContext.Clients.All.SendAsync(
                "ReceiveMessage",
                webhookEvent.CheckSuite.UpdatedAt.ToLocalTime(),
                webhookEvent.CheckSuite.Id.ToString(),
                subject,
                plainTextContent,
                prDetails.Url,                  );

            //var response = await client.SendEmailAsync(msg);
        }

        public static string GetPRCommentTableName(PullRequest pr)
        {
            return "prc" + pr.Id.ToString();
        }
    }
}
