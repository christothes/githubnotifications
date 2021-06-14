using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using GitHubNotifications.Models;
using GitHubNotifications.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitHubNotifications.Server
{
    public class HubWorker : Worker, IHostedService
    {
        private readonly IHubContext<NotificationsHub> _hubContext;
        private readonly TableClient prTable;

        public HubWorker(
            ILogger<HubWorker> logger,
            EventDispatcher dispatcher,
            TableServiceClient tableService,
            IHubContext<NotificationsHub> hub) : base(dispatcher, logger)
        {
            _hubContext = hub;
            prTable = tableService.GetTableClient("prs");
        }

        protected internal override async Task PullRequestReviewCommentEventHandler(PullRequestReviewCommentEvent evt)
        {
            _logger.LogInformation($"{evt.Comment.CreatedAt.ToLocalTime()} PRComment Url: {evt.Comment.HtmlUrl} PR: {evt.PullRequest.HtmlUrl}");

            var model = new CommentModel(
                evt.Comment.Id.ToString(),
                evt.Comment.User.Login,
                evt.Comment.HtmlUrl,
                evt.Comment.CreatedAt,
                evt.Comment.UpdatedAt,
                evt.PullRequest.Title,
                evt.PullRequest.Number.ToString(),
                evt.PullRequest.User.Login,
                evt.Comment.Body,
                evt.Comment.InReplyToId.ToString(),
                "",
                string.Join(";", evt.PullRequest.Labels.Select(l => l.Name)));

            await _hubContext.Clients.All.SendAsync(
                "NewComment", model);
        }

        protected internal override async Task IssueCommentEventHandler(IssueCommentEvent evt)
        {
            _logger.LogInformation($"{evt.Comment.CreatedAt.ToLocalTime()} IssueComment Url: {evt.Comment.HtmlUrl} Issue: {evt.Issue.HtmlUrl}");

            var model = new CommentModel(
                evt.Comment.Id.ToString(),
                evt.Comment.User.Login,
                evt.Comment.HtmlUrl,
                evt.Comment.CreatedAt,
                evt.Comment.UpdatedAt,
                evt.Issue.Title,
                evt.Issue.Number.ToString(),
                evt.Issue.User.Login,
                evt.Comment.Body,
                evt.Comment.InReplyToId.ToString(),
                "",
                string.Join(";", evt.Issue.Labels.Select(l => l.Name)));

            await _hubContext.Clients.All.SendAsync(
                "NewComment", model);
        }

        protected internal override async Task CheckSuiteEventHandler(CheckSuiteEvent evt)
        {
            PREntity prDetails;
            try
            {
                var pr = evt.CheckSuite.PullRequests.FirstOrDefault();
                if (pr != null)
                {
                    prDetails = await prTable.GetEntityAsync<PREntity>(pr.Head.Repo.Name, pr.Number.ToString());
                }
                else
                {
                    evt.CheckSuite.PullRequests.FirstOrDefault();
                    throw new Exception("Could not find PR");
                }
            }
            catch
            {
                _logger.LogWarning($"Could not find PR in cache for check {evt.CheckSuite.CheckRunsUrl}");
                return;
            }
            var conclusion = evt.CheckSuite.Conclusion;



            await _hubContext.Clients.All.SendAsync(
                "CheckStatus",
                evt.CheckSuite.UpdatedAt,
                evt.CheckSuite.Id.ToString(),
                prDetails.Title,
                conclusion,
                prDetails.Url,
                prDetails.Author);
        }

        protected internal override async Task PullRequestEventHandler(PullRequestEvent evt)
        {
            switch (evt.Action)
            {
                case "closed":
                    await _hubContext.Clients.All.SendAsync(
                        "PrClosed",
                        evt.Repository.Name,
                        evt.PullRequest.Number.ToString());
                    break;
                case "labeled":
                case "unlabeled":
                    await _hubContext.Clients.All.SendAsync(
                        "LabelChanged",
                        evt.Repository.Name,
                        evt.PullRequest.Number.ToString(),
                        string.Join(';', evt.PullRequest.Labels.Select(l => l.Name)));
                    break;
                case "review_requested":
                    await _hubContext.Clients.All.SendAsync(
                        "PrReviewRequested",
                        evt.Repository.Name,
                        evt.PullRequest.Number.ToString(),
                        string.Join(';', evt.PullRequest.RequestedReviewers.Select(l => l.Login)));
                    break;
            }
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