using AzureMonitorAlertToSlack.Services.Slack;
using AzureMonitorAlertToSlack.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AzureMonitorAlertToSlack.Services.Implementations;

namespace AzureFunctionAlert2Slack
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            //builder.Services.AddHttpClient();

            builder.Services.AddHttpClient<SlackClient>(c => SlackClient.Configure(c));

            builder.Services.AddSingleton<ISlackClient, SlackClient>();

            builder.Services.AddSingleton<ILogQueryServiceFactory, LogQueryServiceFactory>();
            builder.Services.AddSingleton<IDemuxedAlertHandler, DemuxedAlertInfoHandler>();
            builder.Services.AddSingleton<IAlertInfoFactory, AlertInfoFactory>();

            builder.Services.AddSingleton<ISlackMessageFactory, SlackMessageFactory>();
            builder.Services.AddSingleton<IMessageSender, SlackMessageSender>();

            builder.Services.AddLogging();
            //builder.Services.AddSingleton<ILoggerProvider, MyLoggerProvider>();
        }
    }
}
