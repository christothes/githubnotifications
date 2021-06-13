using System;
using System.Threading;
using System.Threading.Tasks;
using GitHubNotifications.Models;
using Microsoft.Extensions.Logging;

namespace GitHubNotifications.Server
{
    public abstract class Worker : IDisposable
    {
        private readonly EventDispatcher _dispatcher;
        protected CancellationToken _token;
        protected readonly ILogger _logger;

        public Worker(EventDispatcher dispatcher, ILogger logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _token = stoppingToken;
            _dispatcher.Register(PullRequestReviewCommentEventHandler);
            _dispatcher.Register(PullRequestReviewEventHandler);
            _dispatcher.Register(PullRequestEventHandler);
            _dispatcher.Register(IssueCommentEventHandler);
            _dispatcher.Register(IssueEventHandler);
            _dispatcher.Register(CheckSuiteEventHandler);
            _logger.LogInformation($"{this.GetType().Name} Hosted Service running.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected internal abstract Task PullRequestReviewCommentEventHandler(PullRequestReviewCommentEvent evt);
        protected internal abstract Task PullRequestReviewEventHandler(PullRequestReviewEvent evt);
        protected internal abstract Task PullRequestEventHandler(PullRequestEvent evt);
        protected internal abstract Task IssueCommentEventHandler(IssueCommentEvent evt);
        protected internal abstract Task IssueEventHandler(IssueEvent evt);
        protected internal abstract Task CheckSuiteEventHandler(CheckSuiteEvent evt);

        public void Dispose()
        { }

        // edit all comment PR titles

   }
}