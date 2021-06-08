using System.Linq;
using System.Threading.Tasks;
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

        public HubWorker(
            ILogger<HubWorker> logger,
            EventDispatcher dispatcher,
            IHubContext<NotificationsHub> hub) : base(dispatcher, logger)
        {
            _hubContext = hub;
        }

        protected internal override async Task PrCommentEventHandler(PullRequestReviewEvent evt)
        {
            _logger.LogInformation($"{evt.Comment.CreatedAt.ToLocalTime()} PRComment Url: {evt.Comment.HtmlUrl}");

            var model = new CommentModel(
                evt.Comment.Id.ToString(),
                evt.Comment.User.Login,
                evt.Comment.HtmlUrl,
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

        protected internal override async Task IssueEventHandler(IssueEvent evt)
        {
            _logger.LogInformation($"{evt.Comment.CreatedAt.ToLocalTime()} PRComment Url: {evt.Comment.HtmlUrl}");

            var model = new CommentModel(
                evt.Comment.Id.ToString(),
                evt.Comment.User.Login,
                evt.Comment.HtmlUrl,
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
            if (evt.CheckSuite.Conclusion != "failure")
            {
                return;
            }
            PullRequest prDetails;
            try
            {
                prDetails = evt.CheckSuite.PullRequests.FirstOrDefault();
            }
            catch
            {
                _logger.LogWarning("*** Could not find PR in cache ***");
                return;
            }
            var subject = $"Checks {evt.CheckSuite.Conclusion} for PR: {prDetails.Title}";
            var plainTextContent = $"PR: {prDetails.Url}";

            _logger.LogInformation(subject);
            _logger.LogInformation(plainTextContent);

            await _hubContext.Clients.All.SendAsync(
                "CheckStatus",
                evt.CheckSuite.UpdatedAt.ToLocalTime(),
                evt.CheckSuite.Id.ToString(),
                subject,
                plainTextContent,
                prDetails.Url,
                prDetails.User.Login);
        }

        protected internal override Task PullRequestEventHandler(PullRequestEvent evt)
        {
            throw new System.NotImplementedException();
        }
    }
}