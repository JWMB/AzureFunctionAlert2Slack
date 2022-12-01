using AzureMonitorAlertToSlack.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Shouldly;
using AzureMonitorAlertToSlack;
using AzureMonitorAlertToSlack.Services.LogQuery;

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
                Slack = new AzureMonitorAlertToSlack.Services.Slack.SlackSettings
                {
                    DefaultWebhook = "1"
                }
            };
            var flatDict = ObjectToFlatDictionary(appSettings, "AppSettings").ToDictionary(o => o.key, o => o.value);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(flatDict)
                .Build();

            services.AddSingleton<IConfiguration>(config);

            Startup.ConfigureServices(services, config, "test");

            var sp = services.BuildServiceProvider();

            ShouldOrShouldNotThrow(useLogQueryService == false, () => sp.GetRequiredService<ILogAnalyticsQueryService>());

            Should.NotThrow(sp.GetRequiredService<ApplicationInsightsQuerySettings>);
            Should.NotThrow(sp.GetRequiredService<LogAnalyticsQuerySettings>);
            Should.NotThrow(sp.GetRequiredService<IMessageSender>);
            Should.NotThrow(sp.GetRequiredService<IAlertInfoFactory>);

            Should.NotThrow(sp.GetRequiredService<RequestToSlackFunction>);

            void ShouldOrShouldNotThrow(bool shouldThrow, Action action)
            {
                if (shouldThrow)
                    Should.Throw<Exception>(action);
                else
                    Should.NotThrow(action);
            }
        }

        public static IEnumerable<(string key, string value)> ObjectToFlatDictionary(object obj, string path = "")
        {
            var type = obj.GetType();
            var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(o => o.CanWrite);
            foreach (var prop in properties)
            {
                var fullPath = $"{(path.Any() ? $"{path}:" : "")}{prop.Name}";
                var val = prop.GetValue(obj);
                if (val == null)
                    ; // yield return (fullPath, "");
                else if (val.GetType().IsClass && val is not string)
                    foreach (var item in ObjectToFlatDictionary(val, fullPath))
                        yield return item;
                else
                    yield return (fullPath, val.ToString() ?? "");

            }
        }
    }
}
