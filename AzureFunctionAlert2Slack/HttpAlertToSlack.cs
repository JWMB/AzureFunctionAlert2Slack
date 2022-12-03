using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using AzureMonitorAlertToSlack.LogQuery;
using AzureMonitorAlertToSlack.Alerts;
using AzureMonitorAlertToSlack.Slack;

namespace AzureFunctionAlert2Slack
{
    public class HttpAlertToSlack
    {
        private RequestToSlackFunction? function;

        public HttpAlertToSlack(RequestToSlackFunction? function = null)
        {
            this.function = function;
        }

        [FunctionName("HttpAlertToSlack")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            if (function == null)
            {
                log.LogError("Dependency injection didn't work");
                ILogQueryServiceFactory? logQueryServiceFactory =
                    Environment.GetEnvironmentVariable("UseLogQueryService") == "1"
                    ? CreateLogQueryServiceFactory()
                    : null;

                var logger = new LoggerWrapper<RequestToSlackFunction>(log);
                function = new RequestToSlackFunction(
                    new MySummarizedAlertFactory(new MyDemuxedAlertHandler(logQueryServiceFactory)),
                    new SlackClient(SlackClient.Configure(HttpClientFactory.Create()),  new SlackSettings { DefaultWebhook = Environment.GetEnvironmentVariable("slackWebhook") ?? "" }), 
                    new MySlackMessageFactory(),
                    logger);
            }

            return await function.Run(req);
        }


        private class LoggerWrapper<T> : ILogger<T>
        {
            private readonly ILogger log;
            public LoggerWrapper(ILogger log)
            {
                this.log = log;
            }
            public IDisposable BeginScope<TState>(TState state) => log.BeginScope(state);
            public bool IsEnabled(LogLevel logLevel) => log.IsEnabled(logLevel);
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => log.Log(logLevel, eventId, state, exception, formatter);
        }

        private static ILogQueryServiceFactory CreateLogQueryServiceFactory()
        {
            var logQuerySettings = new LogQuerySettings
            {
                ApplicationInsights = new ApplicationInsightsQuerySettings
                {
                    AppId = Environment.GetEnvironmentVariable("ApplicationInsightsAppId") ?? "",
                    ApiKey = Environment.GetEnvironmentVariable("ApplicationInsightsApiKey") ?? ""
                },
                Enabled = true,
                Timeout = 20,
                LogAnalytics = new LogAnalyticsQuerySettings
                {
                    WorkspaceId = Environment.GetEnvironmentVariable("workspaceId") ?? ""
                }
            };
            var la = new LogAnalyticsQueryService(logQuerySettings.LogAnalytics);
            var ai = new AppInsightsQueryService(
                        new AppInsightsQueryService.ApplicationInsightsClient(
                            AppInsightsQueryService.ApplicationInsightsClient.ConfigureClient(HttpClientFactory.Create(), logQuerySettings.ApplicationInsights)));

            return new LogQueryServiceFactory(logQuerySettings, la, ai);
        }
    }
}
