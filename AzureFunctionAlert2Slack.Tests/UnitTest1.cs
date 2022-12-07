using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using AzureMonitorAlertToSlack;
using AzureMonitorAlertToSlack.LogQuery;
using AzureMonitorAlertToSlack.Slack;
using AzureMonitorAlertToSlack.Alerts;
using AzureMonitorCommonAlertSchemaTypes.AlertContexts;
using AzureMonitorCommonAlertSchemaTypes;
using AzureMonitorCommonAlertSchemaTypes.AlertContexts.LogAlertsV2;
using AutoFixture;
using Moq;
using AutoFixture.AutoMoq;

namespace AzureFunctionAlert2Slack.Tests
{
    public class UnitTest1
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Services_UseLogQueryService(bool useLogQueryService)
        {
            var services = new ServiceCollection();

            var appSettings = new AppSettings
            {
                LogQuery = new LogQuerySettings
                {
                    Enabled = useLogQueryService,
                    ApplicationInsights = new ApplicationInsightsQuerySettings
                    {
                        ApiKey = "1",
                        AppId = "1",
                    },
                    LogAnalytics = new LogAnalyticsQuerySettings
                    {
                        WorkspaceId = "1"
                    }
                },
                Slack = new SlackSettings
                {
                    DefaultWebhook = "1"
                }
            };
            var flatDict = ConfigurationHelpers.ObjectToFlatDictionary(appSettings, "AppSettings").ToDictionary(o => o.key, o => o.value);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(flatDict)
                .Build();

            services.AddSingleton<IConfiguration>(config);

            Startup.ConfigureServices(services, config, "test");

            var sp = services.BuildServiceProvider();

            ShouldOrShouldNotThrow(shouldThrow: useLogQueryService == false, () => sp.GetRequiredService<ILogAnalyticsQueryService>());

            Should.NotThrow(sp.GetRequiredService<ApplicationInsightsQuerySettings>);
            Should.NotThrow(sp.GetRequiredService<LogAnalyticsQuerySettings>);

            Should.NotThrow(sp.GetRequiredService<ISlackMessageFactory<SummarizedAlert, SummarizedAlertPart>>);
            Should.NotThrow(sp.GetRequiredService<ISlackClient>);
            Should.NotThrow(sp.GetRequiredService<ISummarizedAlertFactory<SummarizedAlert, SummarizedAlertPart>>);

            Should.NotThrow(sp.GetRequiredService<RequestToSlackFunction>);

            void ShouldOrShouldNotThrow(bool shouldThrow, Action action)
            {
                if (shouldThrow)
                    Should.Throw<Exception>(action);
                else
                    Should.NotThrow(action);
            }
        }

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

            alertHandler.Handled.Title.ShouldBe($"{alert.Data.CustomProperties["titlePrefix"]}{alert.Data.CustomProperties["titleSuffix"]}");
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
            var criteria = new LogQueryCriteria[] { new LogQueryCriteria { SearchQuery = "AppTraces" } };
            ctx.Condition.AllOf = criteria;
            alert.Data.AlertContext = ctx;

            alert.Data.CustomProperties = new Dictionary<string, string>
            {
                { "querySuffix", "| project Timestamp"},
            };

            alertHandler.LogAlertsV2AlertContext(alert, ctx, criteria);

            var expectedQuery = $"{criteria.Single().SearchQuery}\n{alert.Data.CustomProperties["querySuffix"]}";
            Mock.Get(qService).Verify(o => o.GetQueryAsDataTable(It.Is<string>(o => o == expectedQuery), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken?>()),
                Times.Once);
        }
    }
}
