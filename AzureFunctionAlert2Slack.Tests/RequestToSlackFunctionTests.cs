using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Shouldly;
using AzureMonitorAlertToSlack;
using AzureMonitorAlertToSlack.LogQuery;
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
        private IMessageSender messageSender;
        private IAlertInfoFactory alertInfoFactory;

        public RequestToSlackFunctionTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());

            //var alertInfoFactory = fixture.Build<IAlertInfoFactory>().With(o => o.Process(It.IsAny<string>()), Task.FromResult(new List<AlertInfo> { })).Create();
            var mAlertInfoFactory = new Mock<IAlertInfoFactory>();
            mAlertInfoFactory.Setup(o => o.Process(It.IsAny<string>())).Returns(Task.FromResult(new List<AlertInfo> { }));
            alertInfoFactory = mAlertInfoFactory.Object;

            var mMessageSender = new Mock<IMessageSender>();
            mMessageSender.Setup(o => o.SendMessage(It.IsAny<List<AlertInfo>>())).Returns(Task.CompletedTask);
            messageSender = mMessageSender.Object;
        }

        [Fact]
        public async Task RequestToSlackFunction_HappyPath()
        {
            var function = new RequestToSlackFunction(alertInfoFactory, messageSender, fixture.Create<ILogger<RequestToSlackFunction>>());
            var req = await CreateRequest(fixture.Create<Uri>(), "{}");

            var result = await function.Run(req);

            result.ShouldBeAssignableTo<OkObjectResult>();

            Mock.Get(alertInfoFactory).Verify(o => o.Process(It.IsAny<string>()), Times.Once);
            Mock.Get(messageSender).Verify(o => o.SendMessage(It.IsAny<List<AlertInfo>>()), Times.Once);
        }

        [Fact]
        public async Task RequestToSlackFunction_NoBody()
        {
            var function = new RequestToSlackFunction(alertInfoFactory, messageSender, fixture.Create<ILogger<RequestToSlackFunction>>());
            var result = await function.Run(await CreateRequest());

            result.ShouldBeAssignableTo<BadRequestObjectResult>();

            Mock.Get(alertInfoFactory).Verify(o => o.Process(It.IsAny<string>()), Times.Never);
            Mock.Get(messageSender).Verify(o => o.SendMessage(It.IsAny<List<AlertInfo>>()), Times.Never);
        }

        [Fact]
        public async Task RequestToSlackFunction_ProcessException()
        {
            var error = "Some error";
            Mock.Get(alertInfoFactory).Setup(o => o.Process(It.IsAny<string>())).Throws(new Exception(error));

            var function = new RequestToSlackFunction(alertInfoFactory, messageSender, fixture.Create<ILogger<RequestToSlackFunction>>());
            var req = await CreateRequest(content: "{}");

            var result = await function.Run(req);

            result.ShouldBeAssignableTo<BadRequestObjectResult>();
            ((BadRequestObjectResult)result).Value.ShouldBe($"Could not read body: {error}");

            Mock.Get(alertInfoFactory).Verify(o => o.Process(It.IsAny<string>()), Times.Once);
            //Mock.Get(messageSender).Verify(o => o.SendMessage(It.IsAny<List<AlertInfo>>()), Times.Once);
            Mock.Get(messageSender).Verify(o => o.SendMessage(It.Is<List<AlertInfo>>(x => x.Count == 2)), Times.Once);
        }

        private async Task<HttpRequest> CreateRequest(Uri? uri = null, string? content = null)
        {
            return await Helpers.CreateRequest(HttpMethod.Get, 
                uri ?? fixture.Create<Uri>(),
                content == null ? null : new StringContent(content, new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"))); ;
        }
    }
}
