using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using GitHubNotifications.Models;
using System.Threading.Tasks;
using System;
using System.Linq;
using GitHubNotifications.Server;
using System.Text.Json;
using static GitHubNotifications.Shared.TestData;

namespace GitHubNotifications.Tests
{
    public class EventDispatcherTests : TestBase
    {
        private EventDispatcher target;
        Mock<ILogger<EventDispatcher>> loggerMock;
        bool prEventCalled = false;
        bool reviewEventCalled = false;
        bool issueEventCalled = false;
        bool checkEventCalled = false;
        Func<PullRequestReviewEvent, Task> commentFunc;
        Func<PullRequestEvent, Task> prFunc;
        Func<IssueEvent, Task> issueFunc;
        Func<CheckSuiteEvent, Task> checkFunc;

        [SetUp]
        public void Setup()
        {
            loggerMock = new Mock<ILogger<EventDispatcher>>();
            target = new EventDispatcher(loggerMock.Object);
            commentFunc = (e) =>
           {
               reviewEventCalled = true;
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
            checkFunc = (e) =>
            {
                checkEventCalled = true;
                return Task.CompletedTask;
            };
        }

        [Test]
        public async Task RegisterAddsFuncAndFireCallsIt()
        {
            target.Register(commentFunc);
            Assert.AreSame(commentFunc, target.reviewEventSubscribers.FirstOrDefault());

            await target.FireEvent(prCommentEvent);
            Assert.IsTrue(reviewEventCalled);

            target.Register(prFunc);
            Assert.AreSame(prFunc, target.prEventSubscribers.FirstOrDefault());

            await target.FireEvent(prEvent);
            Assert.IsTrue(prEventCalled);

            target.Register(issueFunc);
            Assert.AreSame(issueFunc, target.issueEventSubscribers.FirstOrDefault());

            await target.FireEvent(issueEvent);
            Assert.IsTrue(issueEventCalled);

            target.Register(checkFunc);
            Assert.AreSame(checkFunc, target.checkEventSubscribers.FirstOrDefault());

            await target.FireEvent(checkSuiteEvent);
            Assert.IsTrue(checkEventCalled);
        }

        [TestCaseSource(nameof(Payloads))]
        public async Task ProcessEvent(Dictionary<string, object> elementMap)
        {
            target.Register(commentFunc);
            target.Register(prFunc);
            target.Register(issueFunc);
            target.Register(checkFunc);

            await target.FireEvent(checkSuiteEvent);
            Assert.IsTrue(checkEventCalled);
            var eventPayload = elementMap["content"];
            string evtType = ((GitHubEvent)elementMap["headers"]).XGitHubEvent[0];

            elementMap["content"] = EncodePayload(elementMap["content"]);
            var json = JsonSerializer.Serialize(elementMap);

            await target.ProcessEvent(json);

            switch (eventPayload)
            {
                case ICommentEvent ce:
                    Assert.That(reviewEventCalled == true || issueEventCalled == true);
                    break;
                case PullRequestEvent pre:
                    Assert.IsTrue(prEventCalled);
                    break;
                case CheckSuiteEvent cse:
                    Assert.IsTrue(checkEventCalled);
                    break;
            }
        }
    }
}