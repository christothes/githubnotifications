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
        public static User user = new User { Login = "christothes" };
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
            Number = rnd.Next()
        };

        public static CheckSuiteEvent checkSuiteEvent = new CheckSuiteEvent
        {
            Action = "check_suite",
            CheckSuite = new CheckSuite
            {
                Conclusion = "failure",
                PullRequests = new List<PullRequest>
                {
                    GetPullRequest()
                },
                HeadSha = "sha",
                UpdatedAt = DateTime.Now,
                Id = rnd.Next()
            }
        };

        public static IssueCommentEvent issueEvent = new IssueCommentEvent()
        {
            Issue = new Issue
            {
                Id = 4444,
                User = user,
                Url = "https://github.com/issue/1234",
                PullRequest = new PullRequestIssue
                {
                    Url = "https://github.com/pr/1234"
                },
                Labels = new List<Label>()
            },
            Comment = new Comment
            {
                Body = "issue comment body",
                HtmlUrl = "https://github.com",
                CreatedAt = DateTime.Now,
                InReplyToId = 0,
                User = user
            }
        };
        public static PullRequestEvent prEventUnLabeled = new PullRequestEvent
        {
            Action = "unlabeled",
            PullRequest = GetPullRequest()
        };
        public static PullRequestEvent prEventLabeled = new PullRequestEvent
        {
            Action = "labeled",
            PullRequest = GetPullRequest()
        };
        public static PullRequestEvent prEvent = new PullRequestEvent
        {
            PullRequest = GetPullRequest(),
        };
        public static PullRequestEvent prMergedEvent = new PullRequestEvent
        {
            Action = "closed",
            PullRequest = GetPullRequest(true),
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
        };
    }
}