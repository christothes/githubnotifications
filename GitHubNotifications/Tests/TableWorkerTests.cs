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
        MockLogger<TableWorker> loggerMock;
        Mock<IHubContext<NotificationsHub>> hubMock;
        List<PRComment> prCommentsQueryresponse;

        [SetUp]
        public void Setup()
        {
            prCommentsQueryresponse = GetComments(200);
            commentTableMock = new Mock<TableClient>();
            commentTableMock.Setup(m => m.QueryAsync<PRComment>(It.IsAny<Expression<Func<PRComment, bool>>>(), null, It.IsAny<IEnumerable<string>>(), default))
                .Returns(new MockAsyncPageable<PRComment>(prCommentsQueryresponse))
                .Verifiable();
            tableServiceMock = new Mock<TableServiceClient>();
            loggerMock = new MockLogger<TableWorker>(l => Assert.AreNotEqual(LogLevel.Error, l));
            prsTableMock = new Mock<TableClient>();
            lablesTableMock = new Mock<TableClient>();

            hubMock = new Mock<IHubContext<NotificationsHub>>();
            hubMock.Setup(m => m.Clients.All.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default)).Returns(Task.CompletedTask);
            tableServiceMock.Setup(m => m.GetTableClient("comments")).Returns(commentTableMock.Object);
            tableServiceMock.Setup(m => m.GetTableClient("prs")).Returns(prsTableMock.Object);
            tableServiceMock.Setup(m => m.GetTableClient("labels")).Returns(lablesTableMock.Object);
            dispatcher = new EventDispatcher(Mock.Of<ILogger<EventDispatcher>>());
            target = new TableWorker(loggerMock, dispatcher, tableServiceMock.Object);
        }

        [Test]
        public async Task Startup()
        {
            var source = new CancellationTokenSource();
            Func<PullRequestEvent, Task> prFunc = target.PullRequestEventHandler;
            Func<PullRequestReviewCommentEvent, Task> commentFunc = target.PullRequestReviewCommentEventHandler;
            Func<IssueCommentEvent, Task> issueFunc = target.IssueCommentEventHandler;
            Func<CheckSuiteEvent, Task> checkFunc = target.CheckSuiteEventHandler;

            await target.StartAsync(source.Token);

            Assert.AreEqual(prFunc, dispatcher.pullRequestEventSubscribers.FirstOrDefault());
            Assert.AreEqual(commentFunc, dispatcher.pullRequestReviewCommentEventSubscribers.FirstOrDefault());
            Assert.AreEqual(issueFunc, dispatcher.issueCommentEventSubscribers.FirstOrDefault());
            Assert.AreEqual(checkFunc, dispatcher.checkEventSubscribers.FirstOrDefault());
        }

        public static IEnumerable<object[]> PrEventTests()
        {
            yield return new object[] { prEvent, false };
            yield return new object[] { prClosedEvent, true };
            yield return new object[] { prMergedEvent, true };
        }

        [TestCaseSource(nameof(PrEventTests))]
        public async Task TableWorker_PullRequestEventHandler(PullRequestEvent pre, bool closed)
        {
            await target.PullRequestEventHandler(pre);

            ValidateTable(closed, pre);
        }

        [Test]
        public async Task TableWorker_PullRequestEventHandlerLabledOrUnLabeled([Values(true, false)] bool labeled)
        {
            var pre = labeled ? prEventLabeled : prEventUnLabeled;

            await target.PullRequestEventHandler(pre);

            string labels = string.Join(";", pre.PullRequest.Labels.Select(l => l.Name));

            prsTableMock.Verify();
            commentTableMock.Verify(m => m.SubmitTransactionAsync(
                It.Is<IEnumerable<TableTransactionAction>>(
                    a => a.Count() == 100 &&
                    a.All(i => i.ActionType == TableTransactionActionType.UpdateMerge && ((PRComment)i.Entity).Labels == labels)),
                default), Times.Exactly(2)); ;
        }

        [Test]
        public async Task TableWorker_IssueEventHandler()
        {
            await target.IssueCommentEventHandler(issueCommentEvent);

            commentTableMock.Verify(m => m.UpsertEntityAsync(It.IsAny<PRComment>(), TableUpdateMode.Replace, default));
        }

        [Test]
        public async Task TableWorker_PrCommentEventHandler()
        {
            await target.PullRequestReviewCommentEventHandler(prCommentEvent);

            commentTableMock.Verify(m => m.UpsertEntityAsync(It.IsAny<PRComment>(), TableUpdateMode.Replace, default));
        }

        private void ValidateTable(bool closed, PullRequestEvent pre)
        {
            prsTableMock.Verify(m => m.UpsertEntityAsync(
                It.Is<PREntity>(e => e.PartitionKey == prEvent.Repository.Name), TableUpdateMode.Replace, default));
            {
                commentTableMock.Verify(m => m.SubmitTransactionAsync(
                    It.Is<IEnumerable<TableTransactionAction>>(
                        a => a.Count() == 100 &&
                        a.All(i => i.ActionType == TableTransactionActionType.Delete)),
                    default), closed ? Times.Exactly(2) : Times.Never());
            }
        }

        private List<PRComment> GetComments(int count)
            => Enumerable.Range(0, count).Select(i => new PRComment()).ToList();
    }
}