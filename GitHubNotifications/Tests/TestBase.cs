using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using GitHubNotifications.Models;
using GitHubNotifications.Shared;

namespace GitHubNotifications.Tests
{
    public class TestBase
    {
        public static IEnumerable<object[]> Payloads()
        {
            yield return new object[]{new Dictionary<string, object>
            {
                {"headers", new GitHubEvent{ XGitHubEvent = new[]{"issue_comment"} }},
                {"content", TestData.issueEvent}
            }};

            yield return new object[]{new Dictionary<string, object>
            {
                {"headers", new GitHubEvent{ XGitHubEvent = new[]{"pull_request_review_comment"} }},
                {"content", TestData.prCommentEvent}
            }};

            yield return new object[]{new Dictionary<string, object>
            {
                {"headers", new GitHubEvent{ XGitHubEvent = new[]{"pull_request"} }},
                {"content", TestData.prEvent}
            }};

            yield return new object[]{new Dictionary<string, object>
            {
                {"headers", new GitHubEvent{ XGitHubEvent = new[]{"pull_request"} }},
                {"content", TestData.prMergedEvent}
            }};

            yield return new object[]{new Dictionary<string, object>
            {
                {"headers", new GitHubEvent{ XGitHubEvent = new[]{"check_suite"} }},
                {"content", TestData.checkSuiteEvent}
            }};
        }

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