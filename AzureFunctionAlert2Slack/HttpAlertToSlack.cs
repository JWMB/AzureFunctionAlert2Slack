using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AzureMonitorAlertToSlack.Services.Implementations;
using AzureMonitorAlertToSlack.Services.Slack;
using AzureMonitorAlertToSlack.Services;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Net.Http;
using AzureMonitorAlertToSlack.Services.LogQuery;

namespace AzureFunctionAlert2Slack
{
    public static class HttpAlertToSlack
    {

        //[FunctionName("HttpAlertToSlack")]
        //public static async Task<IActionResult> Run(
        //    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        //    IAlertInfoFactory alertInfoFactory, IMessageSender sender,
        //    ILogger log)
        //{
        //    return await RunInternal(req, alertInfoFactory, sender, log);
        //}

        public static async Task<IActionResult> Run(HttpRequest req, RequestToSlackFunction function, ILogger log)
        {
            return await function.Run(req);
        }

        [FunctionName("HttpAlertToSlack")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            ILogQueryServiceFactory? logQueryServiceFactory = 
                Environment.GetEnvironmentVariable("UseLogQueryService") == "1" 
                ? CreateLogQueryServiceFactory()
                : null;

            var logger = new LoggerWrapper<RequestToSlackFunction>(log);
            var function = new RequestToSlackFunction(
                new AlertInfoFactory(new DemuxedAlertInfoHandler(logQueryServiceFactory)),
                new SlackMessageSender(new SlackClient(SlackClient.Configure(HttpClientFactory.Create())), new SlackMessageFactory()), logger);

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
            var la = new LogAnalyticsQueryService(Environment.GetEnvironmentVariable("workspaceId") ?? "");
            var ai = new AppInsightsQueryService(
                        new AppInsightsQueryService.ApplicationInsightsClient(
                            AppInsightsQueryService.ApplicationInsightsClient.ConfigureClient(HttpClientFactory.Create(),
                            Environment.GetEnvironmentVariable("workspaceId") ?? "",
                            Environment.GetEnvironmentVariable("workspaceId") ?? ""))
                        );

            return new LogQueryServiceFactory(la, ai);
        }
    }
}
