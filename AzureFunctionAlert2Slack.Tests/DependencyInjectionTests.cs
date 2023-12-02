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
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace AzureFunctionAlert2Slack.Tests
{
    public class DependencyInjectionTests
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
            var flatDict = ConfigurationHelpers.ObjectToFlatDictionary(appSettings, "AppSettings").ToDictionary(o => o.key, o => (string?)o.value);

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
        public void StartupFallback_CreateFunction()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization()); ;

            var envVars = ConfigurationHelpers.ObjectToFlatDictionary(fixture.Create<MyAppSettings>()).ToDictionary(o => o.key, o => o.value);
            StartupFallback.CreateFunction(envVars, fixture.Create<ILogger>());
        }

        [Fact]
        public void StartupFallback_FlatDictionaryToJson()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization()); ;
            var appSettings = fixture.Create<AppSettings>();
            
            var flatDict = ConfigurationHelpers.ObjectToFlatDictionary(appSettings, "AppSettings").ToDictionary(o => o.key, o => o.value);
            var json = StartupFallback.FlatDictionaryToJson(flatDict);

            var recreated = JsonConvert.DeserializeObject<AppSettings>(json.ToString());
            recreated.ShouldBeEquivalentTo(appSettings);
        }
    }
}
