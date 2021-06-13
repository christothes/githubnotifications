using System;
using System.Collections.Generic;
using GitHubNotifications.Models;

namespace GitHubNotifications.Shared
{

    public class TestEvent
    {
        public string EventType { get; set; }
    }

    public static class TestData
    {
        private static Random rnd = new Random();

        public static CheckSuiteEvent checkSuiteSuccessdEvent = new CheckSuiteEvent
        {
            Action = "check_suite",
            CheckSuite = new CheckSuite
            {
                Conclusion = "success",
                PullRequests = new List<PullRequestCheck>
                {
                    prCheck
                },
                HeadSha = "sha",
                UpdatedAt = DateTime.Now,
                Id = rnd.Next()
            }
        };
        public static CheckSuiteEvent checkSuiteFailedEvent = new CheckSuiteEvent
        {
            Action = "check_suite",
            CheckSuite = new CheckSuite
            {
                Conclusion = "failure",
                PullRequests = new List<PullRequestCheck>
                {
                    prCheck
                },
                HeadSha = "sha",
                UpdatedAt = DateTime.Now,
                Id = rnd.Next()
            }
        };
        public static IssueEvent issueEvent = new IssueEvent
        {
            Action = "opened",
            Issue = issue,
            Repository = repo,
            Sender = user,
        };
        public static IssueCommentEvent issueCommentEvent = new IssueCommentEvent()
        {
            Issue = issue,
            Comment = comment
        };
        public static PullRequestEvent prEventUnLabeled = new PullRequestEvent
        {
            Action = "unlabeled",
            PullRequest = GetPullRequest(),
            Repository = repo
        };
        public static PullRequestEvent prEventLabeled = new PullRequestEvent
        {
            Action = "labeled",
            PullRequest = GetPullRequest(),
            Repository = repo
        };
        public static PullRequestEvent prEvent = new PullRequestEvent
        {
            PullRequest = GetPullRequest(),
            Repository = repo
        };
         public static PullRequestEvent prClosedEvent = new PullRequestEvent
        {
            Action = "closed",
            PullRequest = GetPullRequest(false),
            Repository = repo
        };
        public static PullRequestEvent prMergedEvent = new PullRequestEvent
        {
            Action = "closed",
            PullRequest = GetPullRequest(true),
            Repository = repo
        };
        public static Repository repo => new Repository
        {
            Name = "azure-sdk-for-net"
        };
        public static List<User> reviewers => new List<User>{
            new User{ Login = "chris"}
        };
        public static PullRequestReviewCommentEvent prCommentEvent = new PullRequestReviewCommentEvent
        {
            PullRequest = GetPullRequest(),
            Comment = new Comment
            {
                Id = rnd.Next(),
                Body = "issue comment body " + Guid.NewGuid().ToString(),
                HtmlUrl = "https://github.com",
                UpdatedAt = DateTime.Now,
                InReplyToId = 0,
                User = user
            },
            Repository = repo
        };
        public static PullRequestReviewEvent prReviewEvent = new PullRequestReviewEvent
        {
            Action = "submitted",
            PullRequest = GetPullRequest(),
            Repository = repo,
            Review = new Review()
        };
        public static User user => new User { Login = "christothes" };
        public static PullRequestCheck prCheck => new PullRequestCheck
        {
            Head = new Head
            {
                Repo = repo
            },
            Id = 3456,
            Number = 0897
        };
        public static PullRequest GetPullRequest(bool merged = false) =>
        new PullRequest
        {
            Merged = merged,
            Title = "pr title " + Guid.NewGuid().ToString(),
            Url = "https://github.com/pr/1234",
            User = user,
            Head = new Head { Sha = "headsha" },
            HtmlUrl = "https://github.com/pr/1234",
            Labels = new List<Label>
            {
                new Label{ Name = "some label"},
                new Label{ Name = "other label"},
            },
            Number = rnd.Next(),
            RequestedReviewers = reviewers
        };
        public static Issue issue => new Issue
        {
            Id = 4444,
            User = user,
            Url = "https://github.com/issue/1234",
            PullRequest = new PullRequestIssue
            {
                Url = "https://github.com/pr/1234"
            },
            Labels = new List<Label>()
        };
        public static Comment comment => new Comment
        {
            Id = 1234,
            Body = "issue comment body",
            HtmlUrl = "https://github.com",
            CreatedAt = DateTime.Now,
            InReplyToId = 0,
            User = user
        };
    }
}