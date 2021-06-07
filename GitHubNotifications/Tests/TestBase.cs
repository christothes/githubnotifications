using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using GitHubNotifications.Models;

namespace GitHubNotifications.Tests
{

    public class TestBase
    {
        public static IEnumerable<object[]> Payloads()
        {
            yield return new object[]{new Dictionary<string, object>
            {
                {"headers", new GitHubEvent{ XGitHubEvent = new[]{"issue_comment"} }},
                {"content", issueEvent}
            }};

            yield return new object[]{new Dictionary<string, object>
            {
                {"headers", new GitHubEvent{ XGitHubEvent = new[]{"pull_request_review_comment"} }},
                {"content", prCommentEvent}
            }};

            yield return new object[]{new Dictionary<string, object>
            {
                {"headers", new GitHubEvent{ XGitHubEvent = new[]{"pull_request"} }},
                {"content", prEvent}
            }};

            yield return new object[]{new Dictionary<string, object>
            {
                {"headers", new GitHubEvent{ XGitHubEvent = new[]{"pull_request"} }},
                {"content", prMergedEvent}
            }};

            yield return new object[]{new Dictionary<string, object>
            {
                {"headers", new GitHubEvent{ XGitHubEvent = new[]{"check_suite"} }},
                {"content", checkSuiteEvent}
            }};
        }

        protected static User user = new User { Login = "somelogin" };
        protected static PullRequest GetPullRequest(bool merged = false) =>
        new PullRequest
        {
            Merged = merged,
            Title = "pr title",
            Url = "https://github.com/pr/1234",
            User = user,
            Head = new Head { Sha = "headsha" },
            HtmlUrl = "https://github.com/pr/1234",
            Labels = new List<Label>
            {
                new Label{ Name = "some label"}
            }
        };

        protected static CheckSuiteEvent checkSuiteEvent = new CheckSuiteEvent
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
                CreatedAt = DateTime.Now,
            }
        };

        protected static IssueEvent issueEvent = new IssueEvent()
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
        protected static PullRequestEvent prEvent = new PullRequestEvent
        {
            PullRequest = GetPullRequest(),
        };
        protected static PullRequestEvent prMergedEvent = new PullRequestEvent
        {
            Action = "closed",
            PullRequest = GetPullRequest(true),
        };
        protected static PullRequestReviewEvent prCommentEvent = new PullRequestReviewEvent
        {
            PullRequest = new PullRequest
            {
                Title = "pr title",
                Url = "https://github.com/pr/1234",
                User = user,
                Head = new Head { Sha = "headsha" },
                HtmlUrl = "https://github.com/pr/1234",
                Labels = new List<Label>
                {
                    new Label{ Name = "some label"}
                }
            },
            Comment = new Comment
            {
                Body = "issue comment body",
                HtmlUrl = "https://github.com",
                CreatedAt = DateTime.Now,
                InReplyToId = 0,
                User = user
            },
        };

        protected static string EncodePayload<T>(T payload)
        {
            string content = JsonSerializer.Serialize(payload);
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(content);
            return System.Convert.ToBase64String(plainTextBytes);
        }


        protected class MockAsyncPageable<T> : AsyncPageable<T>
        {
            private readonly IEnumerable<T> Items;

            internal MockAsyncPageable(IEnumerable<T> items)
            {
                Items = items;
            }
            public override IAsyncEnumerable<Page<T>> AsPages(string continuationToken = null, int? pageSizeHint = null)
            {
                return CratePageResponse(Items);
            }

            internal async IAsyncEnumerable<Page<P>> CratePageResponse<P>(IEnumerable<P> value)
            {
                await Task.Delay(0);
                yield return new MockPage<P>(value);
            }
        }

        protected class MockPage<T> : Page<T>
        {
            private readonly IReadOnlyList<T> InnerValues;
            public override IReadOnlyList<T> Values => InnerValues;

            public override string ContinuationToken => throw new NotImplementedException();

            public override Response GetRawResponse() => throw new NotImplementedException();

            public MockPage(IEnumerable<T> items)
            {
                InnerValues = items.ToList().AsReadOnly();
            }
        }
    }
}