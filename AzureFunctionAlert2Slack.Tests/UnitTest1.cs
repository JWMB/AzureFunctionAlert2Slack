using AzureMonitorAlertToSlack.Services.Implementations;
using AzureMonitorAlertToSlack.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Shouldly;
using Microsoft.Extensions.Logging;
using AutoFixture;

namespace AzureFunctionAlert2Slack.Tests
{
    public class UnitTest1
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Services_UseLogQueryService(bool useLogQueryService)
        {
            var services = new ServiceCollection();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { "UseLogQueryService", useLogQueryService ? "1" : "" },
                    { "workspaceId", "1" },
                    { "ApplicationInsightsAppId", "1" },
                    { "ApplicationInsightsApiKey", "1" },
                }).Build();

            services.AddSingleton<IConfiguration>(config);

            Startup.ConfigureServices(services, config);

            var sp = services.BuildServiceProvider();

            Should.NotThrow(sp.GetRequiredService<IMessageSender>);
            Should.NotThrow(sp.GetRequiredService<IAlertInfoFactory>);

            var func = sp.GetRequiredService<RequestToSlackFunction>();
        }
    }
}