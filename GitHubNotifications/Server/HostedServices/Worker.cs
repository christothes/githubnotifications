using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using GitHubNotifications.Models;

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
            _dispatcher.Register(PrCommentEventHandler);
            _dispatcher.Register(PullRequestEventHandler);
            _dispatcher.Register(IssueEventHandler);
            _dispatcher.Register(CheckSuiteEventHandler);
            _logger.LogInformation($"{this.GetType().Name} Hosted Service running.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected internal abstract Task PrCommentEventHandler(PullRequestReviewEvent evt);
        protected internal abstract Task PullRequestEventHandler(PullRequestEvent evt);
        protected internal abstract Task IssueEventHandler(IssueEvent evt);
        protected internal abstract Task CheckSuiteEventHandler(CheckSuiteEvent evt);

        public void Dispose()
        { }

        /*
                internal async Task<bool> ProcessMessage(WorkerMessage message, CancellationToken stoppingToken)
                {
                    try
                    {
                        return message.MessageType switch
                        {
                            // Delete all PR comments
                            "prMerged" =>
                                await ExecuteEntityAction(e =>
                                    new TableTransactionAction(TableTransactionActionType.Delete, e), message.Properties["author"], message.Properties["prNumber"]),
                            // Handle PR title changes
                            "titleChanged" =>
                                await ExecuteEntityAction(e =>
                                {
                                    e.PrTitle = message.Properties["title"];
                                    return new TableTransactionAction(TableTransactionActionType.Delete, e);
                                }, message.Properties["author"], message.Properties["prNumber"]),
                            // edit all comment PR titles
                            // Handle label discovery for each PR creation
                            // Handle label Add events
                            // add label to labels table
                            // update all comments with labels list
                            // Handle label removal events
                            // update all comments with labels list
                            _ => false,
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                    }
                    return false;
                }

                private async Task<bool> ExecuteEntityAction(Func<PRComment, TableTransactionAction> action, string author, string prNumber)
                {
                    var actions = new List<TableTransactionAction>();
                    await foreach (var comment in GetComments(author, prNumber))
                    {
                        actions.Add(action(comment));
                    }
                    await ChunkActions(actions);
                    return true;
                }
                private async Task ChunkActions(List<TableTransactionAction> actions)
                {
                    int skip = 0;
                    while (skip < actions.Count)
                    {
                        await commentTable.SubmitTransactionAsync(actions.Skip(skip).Take(100));
                        skip += 100;
                    }
                }

                private AsyncPageable<PRComment> GetComments(string author, string prNumber) =>
                    commentTable.QueryAsync<PRComment>(e => e.PartitionKey == author && e.PrNumber == prNumber);
                public void Dispose()
                { }
            */
    }
}