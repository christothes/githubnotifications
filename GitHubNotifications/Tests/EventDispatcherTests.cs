using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GitHubNotifications.Models;
using GitHubNotifications.Server;
using GitHubNotifications.Shared;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using static GitHubNotifications.Shared.TestData;

namespace GitHubNotifications.Tests
{
    public class EventDispatcherTests : TestBase
    {
        private EventDispatcher target;
        Mock<ILogger<EventDispatcher>> loggerMock;
        bool prReviewCommentEventCalled = false;
        bool prReviewEventCalled = false;
        bool prEventCalled = false;
        bool issueCommentEventCalled = false;
        bool issueEventCalled = false;
        bool checkEventCalled = false;
        Func<PullRequestReviewCommentEvent, Task> prReviewCommentFunc;
        Func<PullRequestReviewEvent, Task> prReviewFunc;
        Func<PullRequestEvent, Task> prFunc;
        Func<IssueCommentEvent, Task> issueCommentFunc;
        Func<IssueEvent, Task> issueFunc;
        Func<CheckSuiteEvent, Task> checkFunc;

        [SetUp]
        public void Setup()
        {
            loggerMock = new Mock<ILogger<EventDispatcher>>();
            target = new EventDispatcher(loggerMock.Object);
            prReviewFunc = (e) =>
            {
                prReviewEventCalled = true;
                return Task.CompletedTask;
            };
            prReviewCommentFunc = (e) =>
            {
                prReviewCommentEventCalled = true;
                return Task.CompletedTask;
            };
            prFunc = (e) =>
            {
                prEventCalled = true;
                return Task.CompletedTask;
            };
            issueFunc = (e) =>
            {
                issueEventCalled = true;
                return Task.CompletedTask;
            };
            issueCommentFunc = (e) =>
            {
                issueCommentEventCalled = true;
                return Task.CompletedTask;
            };
            checkFunc = (e) =>
            {
                checkEventCalled = true;
                return Task.CompletedTask;
            };
        }

        [Test]
        public async Task RegisterAddsFuncAndFireCallsIt()
        {
            target.Register(prReviewFunc);
            Assert.AreSame(prReviewFunc, target.pullRequestReviewEventSubscribers.FirstOrDefault());

            await target.FireEvent(prReviewEvent);
            Assert.IsTrue(prReviewEventCalled);

            target.Register(prReviewCommentFunc);
            Assert.AreSame(prReviewCommentFunc, target.pullRequestReviewCommentEventSubscribers.FirstOrDefault());

            await target.FireEvent(prCommentEvent);
            Assert.IsTrue(prReviewCommentEventCalled);

            target.Register(prFunc);
            Assert.AreSame(prFunc, target.pullRequestEventSubscribers.FirstOrDefault());

            await target.FireEvent(prEvent);
            Assert.IsTrue(prEventCalled);

            target.Register(issueCommentFunc);
            Assert.AreSame(issueCommentFunc, target.issueCommentEventSubscribers.FirstOrDefault());

            await target.FireEvent(issueCommentEvent);
            Assert.IsTrue(issueCommentEventCalled);

            target.Register(issueFunc);
            Assert.AreSame(issueFunc, target.issueEventSubscribers.FirstOrDefault());

            await target.FireEvent(issueEvent);
            Assert.IsTrue(issueEventCalled);

            target.Register(checkFunc);
            Assert.AreSame(checkFunc, target.checkEventSubscribers.FirstOrDefault());

            await target.FireEvent(checkSuiteFailedEvent);
            Assert.IsTrue(checkEventCalled);
        }

        [TestCaseSource(nameof(Payloads))]
        public async Task ProcessEvent(Dictionary<string, object> elementMap)
        {
            target.Register(prReviewCommentFunc);
            target.Register(prFunc);
            target.Register(issueCommentFunc);
            target.Register(checkFunc);
            target.Register(issueFunc);
            target.Register(prReviewFunc);

            var eventPayload = elementMap["content"];
            string evtType = ((GitHubEventEnvelope)elementMap["headers"]).XGitHubEvent[0];
            elementMap["content"] = EncodePayload(elementMap["content"]);
            var json = JsonSerializer.Serialize(elementMap);

            await target.ProcessEvent(json);

            switch (eventPayload)
            {
                case PullRequestReviewCommentEvent:
                    Assert.IsTrue(prReviewCommentEventCalled);
                    break;
                case IssueCommentEvent:
                    Assert.IsTrue(issueCommentEventCalled);
                    break;
                case IssueEvent:
                    Assert.IsTrue(issueEventCalled);
                    break;
                case PullRequestEvent:
                    Assert.IsTrue(prReviewEventCalled);
                    break;
                case CheckSuiteEvent:
                    Assert.IsTrue(checkEventCalled);
                    break;
                case PullRequestReviewEvent:
                    Assert.IsTrue(prReviewEventCalled);
                    break;
            }
        }
    }
}