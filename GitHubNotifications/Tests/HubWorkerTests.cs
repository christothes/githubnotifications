using System.Linq;
using System.Threading.Tasks;
using GitHubNotifications.Models;
using GitHubNotifications.Server;
using GitHubNotifications.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using static GitHubNotifications.Shared.TestData;

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
        public async Task HubWorker_PrCommentEventHandler()
        {
            await target.PullRequestReviewCommentEventHandler(prCommentEvent);

            ValidateHub(prCommentEvent);
        }

        [Test]
        public async Task HubWorker_IssueEventHandler()
        {
            await target.IssueCommentEventHandler(issueCommentEvent);

            ValidateHub(issueCommentEvent);
        }

        [Test]
        public async Task HubWorker_CheckEventHandler([Values(true, false)] bool success)
        {
            var evt = success switch
            {
                true => checkSuiteSuccessdEvent,
                false => checkSuiteFailedEvent
            };

            await target.CheckSuiteEventHandler(evt);
            hubMock.Verify(m => m.Clients.All.SendCoreAsync(
                "CheckStatus",
                It.Is<object[]>(oo => oo.Length == 6),
                default), success ? Times.Never : Times.Once);
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