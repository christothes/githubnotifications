using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Azure.Data.Tables;
using System.Collections.Generic;
using GitHubNotifications.Models;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using GitHubNotifications.Shared;
using System.Linq;
using System.Linq.Expressions;
using Azure;
using System.Threading;

namespace GitHubNotifications.Tests
{
    public class EventDispatcherTests
    {
        private EventDispatcher target;
        Mock<TableClient> commentTableMock;
        Mock<TableClient> prstableMock;
        Mock<TableServiceClient> tableServiceMock;
        Mock<ILogger<EventDispatcher>> loggerMock;
        Mock<IHubContext<NotificationsHub>> hubMock;
        List<PRComment> prCommentsQueryresponse = new List<PRComment>
        {
            new PRComment(), new PRComment(), new PRComment()
        };

        [SetUp]
        public void Setup()
        {
            commentTableMock = new Mock<TableClient>();
            commentTableMock.Setup(m => m.QueryAsync<PRComment>(It.IsAny<Expression<Func<PRComment, bool>>>(), null, null, default))
                .Returns(new MockAsyncPageable<PRComment>(prCommentsQueryresponse))
                .Verifiable();
            tableServiceMock = new Mock<TableServiceClient>();
            loggerMock = new Mock<ILogger<EventDispatcher>>();
            prstableMock = new Mock<TableClient>();
            hubMock = new Mock<IHubContext<NotificationsHub>>();
            hubMock.Setup(m => m.Clients.All.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default)).Returns(Task.CompletedTask);

            tableServiceMock.Setup(m => m.GetTableClient("comments")).Returns(commentTableMock.Object);
            tableServiceMock.Setup(m => m.GetTableClient("prs")).Returns(prstableMock.Object);
            target = new EventDispatcher(loggerMock.Object, hubMock.Object, tableServiceMock.Object);
        }

        [TestCaseSource(nameof(Payloads))]
        public async Task ProcessEvent(Dictionary<string, object> elementMap)
        {
            var eventPayload = elementMap["content"];
            string evtType = ((GitHubEvent)elementMap["headers"]).XGitHubEvent[0];

            elementMap["content"] = EncodePayload(elementMap["content"]);
            var json = JsonSerializer.Serialize(elementMap);

            await target.ProcessEvent(json);

            switch (eventPayload)
            {
                case ICommentEvent ce:
                    validateCommentEvent(ce);
                    break;
                case PullRequestEvent pre:
                    if (pre.Action == "closed" && pre.PullRequest.Merged)
                    {
                        prstableMock.Verify(m => m.DeleteEntityAsync(
                            It.IsAny<string>(),
                            pre.PullRequest.Head.Sha,
                            default,
                            default), Times.Once);
                        prstableMock.Verify();
                        commentTableMock.Verify(m => m.DeleteEntityAsync(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            default,
                            default), Times.Exactly(prCommentsQueryresponse.Count));
                    }
                    else
                    {
                        prstableMock.Verify(m => m.UpsertEntityAsync(
                            It.Is<PREntity>(pr => pr.RowKey == pre.PullRequest.Head.Sha),
                            TableUpdateMode.Replace,
                            default), Times.Once);
                    }
                    break;
            }
        }

        private void validateCommentEvent(ICommentEvent commentEvent)
        {
            commentTableMock.Verify(m => m.UpsertEntityAsync(It.IsAny<PRComment>(), TableUpdateMode.Replace, default));
            hubMock.Verify(m => m.Clients.All.SendCoreAsync(
                "NewComment",
                It.Is<object[]>(oo => ((CommentModel)oo.Single()).Body == commentEvent.Comment.Body),
                default));
        }

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
        }

        private static User user = new User { Login = "somelogin" };
        private static IssueEvent issueEvent = new IssueEvent()
        {
            Issue = new Issue
            {
                Id = 4444,
                User = user,
                Url = "https://github.com/issue/1234",
                PullRequest = new PullRequestIssue
                {
                    Url = "https://github.com/pr/1234"
                }
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
        private static PullRequestEvent prEvent = new PullRequestEvent
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
        };
        private static PullRequestEvent prMergedEvent = new PullRequestEvent
        {
            Action = "closed",
            PullRequest = new PullRequest
            {
                Merged = true,
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
        };
        private static PullRequestReviewEvent prCommentEvent = new PullRequestReviewEvent
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

        private static string EncodePayload<T>(T payload)
        {
            string content = JsonSerializer.Serialize(payload);
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(content);
            return System.Convert.ToBase64String(plainTextBytes);
        }


        private class MockAsyncPageable<T> : AsyncPageable<T>
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

        private class MockPage<T> : Page<T>
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