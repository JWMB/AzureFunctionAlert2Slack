using AzureMonitorAlertToSlack.Services.Slack;
using AzureMonitorAlertToSlack.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using AzureMonitorAlertToSlack.Services.Implementations;
using AzureMonitorAlertToSlack.Services.LogQuery;
using Microsoft.Extensions.Configuration;
using System;
using AzureMonitorAlertToSlack;

[assembly: FunctionsStartup(typeof(AzureFunctionAlert2Slack.Startup))]
namespace AzureFunctionAlert2Slack
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            ConfigureServices(builder.Services, builder.GetContext().Configuration);
        }

        public static void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            services.AddHttpClient<SlackClient>(c => SlackClient.Configure(c));
            services.AddSingleton<ISlackClient, SlackClient>();

            TypedConfiguration.ConfigureTypedConfiguration<AppSettings>(services, config, "AppSettings");
            if (config.GetValue("UseLogQueryService", "") == "1")
            {
                services.AddHttpClient<AppInsightsQueryService.ApplicationInsightsClient>(
                    (sp, c) => AppInsightsQueryService.ApplicationInsightsClient.ConfigureClient(c, GetConfigValue(sp, "ApplicationInsightsAppId"), GetConfigValue(sp, "ApplicationInsightsApiKey")));

                services.AddSingleton<IAppInsightsQueryService, AppInsightsQueryService>();
                services.AddSingleton<ILogAnalyticsQueryService>(sp => new LogAnalyticsQueryService(GetConfigValue(sp, "workspaceId")));

                services.AddSingleton<ILogQueryServiceFactory, LogQueryServiceFactory>();
            }

            services.AddSingleton<IDemuxedAlertHandler, DemuxedAlertInfoHandler>();
            services.AddSingleton<IAlertInfoFactory, AlertInfoFactory>();

            services.AddSingleton<ISlackMessageFactory, SlackMessageFactory>();
            services.AddSingleton<IMessageSender, SlackMessageSender>();

            services.AddSingleton<RequestToSlackFunction>();

            services.AddLogging();

            string GetConfigValue(IServiceProvider sp, string name, string? defaultValue = null)
            {
                var val = sp.GetRequiredService<IConfiguration>().GetValue(name, defaultValue);
                if (val == null)
                    throw new ArgumentNullException(name);
                return val;
            }
        }
    }
}
