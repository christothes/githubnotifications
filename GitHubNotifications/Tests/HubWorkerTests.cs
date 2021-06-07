using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using GitHubNotifications.Models;
using GitHubNotifications.Server;
using GitHubNotifications.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace GitHubNotifications.Tests
{

    public class HubWorkerTests : TestBase
    {
        private HubWorker target;
        Mock<EventDispatcher> dispatcherMock;
        Mock<IHubContext<NotificationsHub>> hubMock;

        [SetUp]
        public void Setup()
        {
            hubMock = new Mock<IHubContext<NotificationsHub>>();
            hubMock.Setup(m => m.Clients.All.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default)).Returns(Task.CompletedTask);
            dispatcherMock = new Mock<EventDispatcher>();
            target = new HubWorker(Mock.Of<ILogger<HubWorker>>(), dispatcherMock.Object, hubMock.Object);
        }

        [Test]
        public async Task PrCommentEventHandler()
        {
            await target.PrCommentEventHandler(prCommentEvent);

            ValidateHub(prCommentEvent);
        }

        [Test]
        public async Task IssueEventHandler()
        {
            await target.IssueEventHandler(issueEvent);

            ValidateHub(issueEvent);
        }

        [Test]
        public async Task CheckEventHandlerNotFailedCheck()
        {
            await target.CheckSuiteEventHandler(new CheckSuiteEvent { CheckSuite = new CheckSuite { Conclusion = "Success" } });

            hubMock.Verify(m => m.Clients.All.SendCoreAsync(
                "CheckStatus",
                It.Is<object[]>(oo => oo.Length == 6),
                default), Times.Never);
        }
        [Test]
        public async Task CheckEventHandlerFailedCheck()
        {
            await target.CheckSuiteEventHandler(checkSuiteEvent);

            hubMock.Verify(m => m.Clients.All.SendCoreAsync(
                "CheckStatus",
                It.Is<object[]>(oo => oo.Length == 6),
                default));
        }

        private void ValidateHub(ICommentEvent commentEvent)
        {
            hubMock.Verify(m => m.Clients.All.SendCoreAsync(
                "NewComment",
                It.Is<object[]>(oo => ((CommentModel)oo.Single()).Body == commentEvent.Comment.Body),
                default));
        }
    }
}