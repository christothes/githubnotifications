using System;
using GitHubNotifications.Models;

namespace GitHubNotifications.Server
{
    public class PullRequestEventArgs : EventArgs
    {
        public PullRequestEventArgs(PullRequestEvent prEvent)
        {
            Event = prEvent;
        }
        public PullRequestEvent Event { get; }
    }
    public class PRCommentEventArgs : EventArgs
    {
        public PRCommentEventArgs(PullRequestReviewEvent reviewEvent)
        {
            Event = reviewEvent;
        }
        public PullRequestReviewEvent Event { get; }
    }
    public class CheckSuiteEventArgs : EventArgs
    {
        public CheckSuiteEventArgs(CheckSuiteEvent checkEvent)
        {
            Event = checkEvent;
        }
        public CheckSuiteEvent Event { get; }
    }
}