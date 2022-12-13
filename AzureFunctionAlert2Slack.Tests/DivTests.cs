using AutoFixture.AutoMoq;
using AutoFixture;
using AzureMonitorAlertToSlack.LogQuery;
using AzureMonitorCommonAlertSchemaTypes.AlertContexts.LogAlertsV2;
using AzureMonitorCommonAlertSchemaTypes.AlertContexts;
using AzureMonitorCommonAlertSchemaTypes;
using Moq;
using Shouldly;

namespace AzureFunctionAlert2Slack.Tests
{
    public class DivTests
    {
        [Fact]
        public void MyDemuxedAlertHandler_TitleAffices()
        {
            var alertHandler = new MyDemuxedAlertHandler(null);

            var alert = new Alert();
            var ctx = new LogAlertsV2AlertContext();
            ctx.Condition.AllOf = Array.Empty<IConditionPart>();
            alert.Data.AlertContext = ctx;

            alert.Data.CustomProperties = new Dictionary<string, string>
            {
                { "titlePrefix", "Prefix: "},
                { "titleSuffix", " - suffix"}
            };

            alertHandler.LogAlertsV2AlertContext(alert, ctx);

            alertHandler.Result.Title.ShouldBe($"{alert.Data.CustomProperties["titlePrefix"]}{alert.Data.CustomProperties["titleSuffix"]}");
        }

        [Fact]
        public void MyDemuxedAlertHandler_QuerySuffix()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var qService = fixture.Create<ILogQueryService>();

            //var qFactory = fixture.Build<ILogQueryServiceFactory>()
            //    .With(o => o.CreateLogQueryService(It.IsAny<string>()), () => qService)
            //    .Create();
            var mQFactory = new Mock<ILogQueryServiceFactory>();
            mQFactory.Setup(o => o.CreateLogQueryService(It.IsAny<string>())).Returns(qService);

            var alertHandler = new MyDemuxedAlertHandler(mQFactory.Object);

            var alert = new Alert();
            var ctx = new LogAlertsV2AlertContext();

            ctx.Properties = new Dictionary<string, string>
            {
                { "querySuffix", "| project Timestamp"},
            };

            var searchQuery = "AppTraces";
            var criteria = new LogQueryCriteria[] { new LogQueryCriteria { SearchQuery = searchQuery } };
            ctx.Condition.AllOf = criteria;
            alert.Data.AlertContext = ctx;

            alertHandler.LogAlertsV2AlertContext(alert, ctx, criteria);

            var expectedQuery = $"{searchQuery}\n{ctx.Properties["querySuffix"]}";
            // Note: For a consumer, it's unexpected that the actual property has changed...
            criteria.Single().SearchQuery.ShouldBe(expectedQuery);
            Mock.Get(qService).Verify(o => o.GetQueryAsDataTable(It.Is<string>(o => o == expectedQuery), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken?>()),
                Times.Once);
        }

        [Theory]
        [InlineData("Sev3", "#0872c4")]
        [InlineData("Sev2", "#fbd023")]
        public void MyDemuxedAlertHandler_AlertColorFromSeverity(string severity, string expectedColor)
        {
            var alertHandler = new MyDemuxedAlertHandler(null);

            var alert = new Alert();
            var ctx = new LogAnalyticsAlertContext();

            alert.Data.AlertContext = ctx;
            alert.Data.Essentials.Severity = severity;

            alertHandler.LogAnalyticsAlertContext(alert, ctx);

            alertHandler.Result.Parts.Select(o => o.Color).Distinct().Single().ShouldBe(expectedColor);
        }

        [Fact]
        public void MyDemuxedAlertHandler_AlertColorFromProperty()
        {
            var alertHandler = new MyDemuxedAlertHandler(null);

            var alert = new Alert();
            var ctx = new LogAlertsV2AlertContext();
            ctx.Condition.AllOf = new LogQueryCriteria[] { };
            var color = "#aabbcc";
            ctx.Properties = new Dictionary<string, string> { { "color", color } };

            alert.Data.AlertContext = ctx;
            alert.Data.Essentials.Severity = "Sev3";

            alertHandler.LogAlertsV2AlertContext(alert, ctx);

            alertHandler.Result.Parts.Select(o => o.Color).Distinct().Single().ShouldBe(color);
        }

        [Fact]
        public void MyDemuxedAlertHandler_AiQueryTransform()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var qService = fixture.Create<ILogQueryService>();

            var mQFactory = new Mock<ILogQueryServiceFactory>();
            mQFactory.Setup(o => o.CreateLogQueryService(It.IsAny<string>())).Returns(qService);

            var alertHandler = new MyDemuxedAlertHandler(mQFactory.Object);

            var alert = new Alert();
            var ctx = new LogAlertsV2AlertContext();
            ctx.Condition.AllOf = new LogQueryCriteria[] { new LogQueryCriteria { SearchQuery = @"
traces
//commented
".Trim().Replace("\r", "") } };
            ctx.Properties = new Dictionary<string, string> { { "queryTransform", "uncomment lines" } };
            alert.Data.AlertContext = ctx;

            alertHandler.LogAlertsV2AlertContext(alert, ctx, (LogQueryCriteria[])ctx.Condition.AllOf);

            Mock.Get(qService).Verify(o => o.GetQueryAsDataTable("traces\ncommented", It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
