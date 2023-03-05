using Shouldly;
using AzureMonitorAlertToSlack.Slack;
using AzureMonitorAlertToSlack.Alerts;
using AutoFixture;
using Microsoft.AspNetCore.Http;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctionAlert2Slack.Tests
{
    public class RequestToSlackFunctionTests
    {
        private IFixture fixture;
        private ISlackClient slackClient;
        private ISlackMessageFactoryTyped messageFactory;
        private ISummarizedAlertFactoryTyped summaryFactory;
        private RequestToSlackFunction function;

        public RequestToSlackFunctionTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());

            // TODO: How to make this work?
            //var summaryFactory = fixture.Build<IAlertInfoFactory>().With(o => o.Process(It.IsAny<string>()), Task.FromResult(new List<AlertInfo> { })).Create();
            var mSummaryFactory = new Mock<ISummarizedAlertFactoryTyped>();
            mSummaryFactory.Setup(o => o.Process(It.IsAny<string>())).Returns(Task.FromResult(new SummarizedAlert()));
            summaryFactory = mSummaryFactory.Object;

            var mSlackClient = new Mock<ISlackClient>();
            mSlackClient.Setup(o => o.Send(It.IsAny<object>(), It.IsAny<string>())).Returns(Task.FromResult(""));
            slackClient = mSlackClient.Object;

            var mMessageFactory = new Mock<ISlackMessageFactoryTyped>();
            mMessageFactory.Setup(o => o.CreateMessages(It.IsAny<SummarizedAlert>())).Returns(new List<SlackNet.WebApi.Message> { new SlackNet.WebApi.Message() });
            messageFactory = mMessageFactory.Object;

            function = new RequestToSlackFunction(summaryFactory, slackClient, messageFactory, fixture.Create<DebugSettings>(), fixture.Create<ILogger<RequestToSlackFunction>>());
        }

        [Fact]
        public async Task RequestToSlackFunction_HappyPath()
        {
            var result = await function.Run(await CreateRequest(fixture.Create<Uri>(), "{}"));

            result.ShouldBeAssignableTo<OkObjectResult>();

            Mock.Get(summaryFactory).Verify(o => o.Process(It.IsAny<string>()), Times.Once);
            Mock.Get(messageFactory).Verify(o => o.CreateMessages(It.IsAny<SummarizedAlert>()), Times.Once);
        }

        [Fact]
        public async Task RequestToSlackFunction_CustomWebhook()
        {
            var customSlackWebhook = fixture.Create<string>();
            Mock.Get(summaryFactory).Setup(o => o.Process(It.IsAny<string>()))
                .Returns(Task.FromResult(new SummarizedAlert { CustomProperties = new Dictionary<string, string> { { "slackWebhook", customSlackWebhook } } }));

            var result = await function.Run(await CreateRequest(fixture.Create<Uri>(), "{}"));

            result.ShouldBeAssignableTo<OkObjectResult>();
            Mock.Get(slackClient).Verify(o => o.Send(It.IsAny<object>(), customSlackWebhook), Times.Once);
        }

        [Fact]
        public async Task RequestToSlackFunction_NoBody()
        {
            var result = await function.Run(await CreateRequest());

            result.ShouldBeAssignableTo<BadRequestObjectResult>();

            Mock.Get(summaryFactory).Verify(o => o.Process(It.IsAny<string>()), Times.Never);
            Mock.Get(slackClient).Verify(o => o.Send(It.IsAny<object>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RequestToSlackFunction_ProcessException()
        {
            var error = "Some error";
            Mock.Get(summaryFactory).Setup(o => o.Process(It.IsAny<string>())).Throws(new Exception(error));

            var result = await function.Run(await CreateRequest(content: "{}"));

            result.ShouldBeAssignableTo<BadRequestObjectResult>();
            ((BadRequestObjectResult)result).Value.ShouldBe($"Could not read body: {error}");

            Mock.Get(summaryFactory).Verify(o => o.Process(It.IsAny<string>()), Times.Once);
            Mock.Get(slackClient).Verify(o => o.Send(It.IsAny<object>(), It.IsAny<string>()), Times.Once);
        }

        private async Task<HttpRequest> CreateRequest(Uri? uri = null, string? content = null)
        {
            return await Helpers.CreateRequest(HttpMethod.Get, 
                uri ?? fixture.Create<Uri>(),
                content == null ? null : new StringContent(content, new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"))); ;
        }
    }
}
