using AzureMonitorAlertToSlack.Services.Slack;
using AzureMonitorAlertToSlack.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using AzureMonitorAlertToSlack.Services.Implementations;
using AzureMonitorAlertToSlack.Services.LogQuery;
using Microsoft.Extensions.Configuration;
using System;
using AzureMonitorAlertToSlack;
using System.Collections.Generic;
using System.Linq;

[assembly: FunctionsStartup(typeof(AzureFunctionAlert2Slack.Startup))]
namespace AzureFunctionAlert2Slack
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var context = builder.GetContext();
            ConfigureServices(builder.Services, context.Configuration, context.EnvironmentName, context.ApplicationRootPath);
        }

        public static void ConfigureServices(IServiceCollection services, IConfiguration config, string environmentName, string? applicationRootPath = null)
        {
            services.AddHttpClient<SlackClient>(c => SlackClient.Configure(c));
            services.AddSingleton<ISlackClient, SlackClient>();

            if (environmentName == "Development")
            {
                // Hmm, debug runtime path is C:\Users\xxxxx\AppData\Local\AzureFunctionsTools\Releases\4.29.0\cli_x64\appsettings.development.json
                var filepath = System.IO.Path.Join(applicationRootPath, $"appsettings.{environmentName.ToLower()}.json");
                if (System.IO.File.Exists(filepath))
                {
                    config = new ConfigurationBuilder()
                        .AddJsonFile(filepath)
                        .AddConfiguration(config)
                        .Build();
                }
            }

            var appSettings = TypedConfiguration.ConfigureTypedConfiguration<AppSettings>(services, config, "AppSettings");
            if (appSettings.LogQuery?.Enabled == true)
            {
                services.AddHttpClient<AppInsightsQueryService.ApplicationInsightsClient>(
                    (sp, c) => AppInsightsQueryService.ApplicationInsightsClient.ConfigureClient(c, sp.GetRequiredService<ApplicationInsightsQuerySettings>()));

                services.AddSingleton<IAppInsightsQueryService, AppInsightsQueryService>();
                services.AddSingleton<ILogAnalyticsQueryService>(sp => new LogAnalyticsQueryService(sp.GetRequiredService<LogAnalyticsQuerySettings>()));

                services.AddSingleton<ILogQueryServiceFactory, LogQueryServiceFactory>();
            }

            services.AddSingleton<IDemuxedAlertHandler, DemuxedAlertInfoHandler>();
            services.AddSingleton<IAlertInfoFactory, AlertInfoFactory>();

            services.AddSingleton<ISlackMessageFactory, SlackMessageFactory>();
            services.AddSingleton<IMessageSender, SlackMessageSender>();

            services.AddSingleton<RequestToSlackFunction>();

            services.AddLogging();
        }
    }
}
