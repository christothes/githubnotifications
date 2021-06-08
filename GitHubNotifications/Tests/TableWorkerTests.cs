using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using GitHubNotifications.Models;
using GitHubNotifications.Server;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using static GitHubNotifications.Shared.TestData;

namespace GitHubNotifications.Tests
{

    public class TableWorkerTests : TestBase
    {
        private TableWorker target;
        EventDispatcher dispatcher;
        Mock<TableClient> commentTableMock;
        Mock<TableClient> prsTableMock;
        Mock<TableClient> lablesTableMock;
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
            commentTableMock.Setup(m => m.QueryAsync<PRComment>(It.IsAny<Expression<Func<PRComment, bool>>>(), null, It.IsAny<IEnumerable<string>>(), default))
                .Returns(new MockAsyncPageable<PRComment>(prCommentsQueryresponse))
                .Verifiable();
            tableServiceMock = new Mock<TableServiceClient>();
            loggerMock = new Mock<ILogger<EventDispatcher>>();
            prsTableMock = new Mock<TableClient>();
            lablesTableMock = new Mock<TableClient>();

            hubMock = new Mock<IHubContext<NotificationsHub>>();
            hubMock.Setup(m => m.Clients.All.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default)).Returns(Task.CompletedTask);
            tableServiceMock.Setup(m => m.GetTableClient("comments")).Returns(commentTableMock.Object);
            tableServiceMock.Setup(m => m.GetTableClient("prs")).Returns(prsTableMock.Object);
            tableServiceMock.Setup(m => m.GetTableClient("labels")).Returns(lablesTableMock.Object);
            dispatcher = new EventDispatcher(Mock.Of<ILogger<EventDispatcher>>());
            target = new TableWorker(Mock.Of<ILogger<TableWorker>>(), dispatcher, tableServiceMock.Object);
        }

        [Test]
        public async Task Startup()
        {
            var source = new CancellationTokenSource();
            Func<PullRequestEvent, Task> prFunc = target.PullRequestEventHandler;
            Func<PullRequestReviewEvent, Task> commentFunc = target.PrCommentEventHandler;
            Func<IssueEvent, Task> issueFunc = target.IssueEventHandler;
            Func<CheckSuiteEvent, Task> checkFunc = target.CheckSuiteEventHandler;

            await target.StartAsync(source.Token);

            Assert.AreEqual(prFunc, dispatcher.prEventSubscribers.FirstOrDefault());
            Assert.AreEqual(commentFunc, dispatcher.reviewEventSubscribers.FirstOrDefault());
            Assert.AreEqual(issueFunc, dispatcher.issueEventSubscribers.FirstOrDefault());
            Assert.AreEqual(checkFunc, dispatcher.checkEventSubscribers.FirstOrDefault());
        }

        [Test]
        public async Task PullRequestEventHandler([Values(true, false)] bool merged)
        {
            var pre = merged ? prMergedEvent : prEvent;

            await target.PullRequestEventHandler(pre);

            ValidateTable(merged, pre);
        }

        [Test]
        public async Task PullRequestEventHandlerLabledOrUnLabeled([Values(true, false)] bool labeled)
        {
            var pre = labeled ? prEventLabeled : prEventUnLabeled;

            await target.PullRequestEventHandler(pre);

            prsTableMock.Verify();
            commentTableMock.Verify(m => m.SubmitTransactionAsync(
                It.Is<IEnumerable<TableTransactionAction>>(a => a.Count() == prCommentsQueryresponse.Count),
                default), Times.Once);
        }

        [Test]
        public async Task IssueEventHandler()
        {
            await target.IssueEventHandler(issueEvent);

            commentTableMock.Verify(m => m.UpsertEntityAsync(It.IsAny<PRComment>(), TableUpdateMode.Replace, default));
        }

        [Test]
        public async Task PrCommentEventHandler()
        {
            await target.PrCommentEventHandler(prCommentEvent);

            commentTableMock.Verify(m => m.UpsertEntityAsync(It.IsAny<PRComment>(), TableUpdateMode.Replace, default));
        }

        private void ValidateTable(bool merged, PullRequestEvent pre)
        {
            if (merged)
            {
                prsTableMock.Verify(m => m.DeleteEntityAsync(
                    It.IsAny<string>(),
                    pre.PullRequest.Head.Sha,
                    default,
                    default), Times.Once);
                prsTableMock.Verify();
                commentTableMock.Verify(m => m.SubmitTransactionAsync(
                    It.Is<IEnumerable<TableTransactionAction>>(a => a.Count() == prCommentsQueryresponse.Count),
                    default), Times.Once);
            }
            else
            {
                prsTableMock.Verify(m => m.UpsertEntityAsync(
                    It.Is<PREntity>(pr => pr.RowKey == pre.PullRequest.Head.Sha),
                    TableUpdateMode.Replace,
                    default), Times.Once);
            }
        }
    }
}