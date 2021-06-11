using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GitHubNotifications.Models;
using Microsoft.Extensions.Logging;

namespace GitHubNotifications.Server
{
    public class EventDispatcher
    {
        private readonly ILogger<EventDispatcher> _logger;
        internal ConcurrentBag<Func<PullRequestReviewCommentEvent, Task>> reviewEventSubscribers = new ConcurrentBag<Func<PullRequestReviewCommentEvent, Task>>();
        internal ConcurrentBag<Func<PullRequestEvent, Task>> prEventSubscribers = new ConcurrentBag<Func<PullRequestEvent, Task>>();
        internal ConcurrentBag<Func<CheckSuiteEvent, Task>> checkEventSubscribers = new ConcurrentBag<Func<CheckSuiteEvent, Task>>();
        internal ConcurrentBag<Func<IssueCommentEvent, Task>> issueEventSubscribers = new ConcurrentBag<Func<IssueCommentEvent, Task>>();

        protected EventDispatcher() { }

        public EventDispatcher(ILogger<EventDispatcher> logger)
        {
            _logger = logger;
        }

        public void Register(Func<PullRequestReviewCommentEvent, Task> func)
        {
            reviewEventSubscribers.Add(func);
        }

        public void Register(Func<PullRequestEvent, Task> func)
        {
            prEventSubscribers.Add(func);
        }

        public void Register(Func<CheckSuiteEvent, Task> func)
        {
            checkEventSubscribers.Add(func);
        }
        public void Register(Func<IssueCommentEvent, Task> func)
        {
            issueEventSubscribers.Add(func);
        }
        internal async Task FireEvent<T>(T evt)
        {
            var tasks = evt switch
            {
                PullRequestReviewCommentEvent r => reviewEventSubscribers.ToList().Select(f => f(r)),
                PullRequestEvent pr => prEventSubscribers.ToList().Select(f => f(pr)),
                CheckSuiteEvent c => checkEventSubscribers.ToList().Select(f => f(c)),
                IssueCommentEvent i => issueEventSubscribers.ToList().Select(f => f(i)),
                _ => throw new InvalidOperationException($"Event handler uknknow: {evt.GetType().FullName}")
            };
            await Task.WhenAll(tasks);
        }

        internal async Task ProcessEvent(string eventBody)
        {
            var elementMap = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(eventBody);
            await ProcessEvent(elementMap);
        }

        internal async Task ProcessEvent(Dictionary<string, JsonElement> elementMap)
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
                            "pull_request_review_comment" => typeof(PullRequestReviewCommentEvent),
                            "issue_comment" => typeof(IssueCommentEvent),
                            //"pull_request_review" => typeof(PullRequestReviewEvent),
                            _ => null,
                        };
                        if (webhookType == null)
                        {
                            _logger.LogError($"unexpected webhook type\n{eventType}");
                            continue;
                        }
                        var webhookObj = JsonSerializer.Deserialize(decoded, webhookType);
                        var logMsg = webhookObj switch
                        {
                            PullRequestReviewCommentEvent e => $"{nameof(e)} action: '{e.Action}'",
                            PullRequestEvent e => $"{nameof(e)} action: '{e.Action}'",
                            CheckSuiteEvent e => $"{nameof(e)} action: '{e.Action}'",
                            IssueCommentEvent e => $"{nameof(e)} action: '{e.Action}'",
                            _ => "unknown event type"
                        };
                        await FireEvent(webhookObj);
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
    }
}