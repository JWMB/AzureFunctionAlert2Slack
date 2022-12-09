using AzureMonitorAlertToSlack.LogQuery;
using AzureMonitorAlertToSlack.Slack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Linq;
using System.Net.Http;

namespace AzureFunctionAlert2Slack
{
    public static class StartupFallback
    {
        public static RequestToSlackFunction CreateFunction(IDictionary envVariables, ILogger log)
        {
            var settings = FlatDictionaryToJson(envVariables).ToObject<MyAppSettings>();

            //var recreated = JsonConvert.DeserializeObject<AppSettings>();

            //var settings = new MyAppSettings();
            //FillWithSettings(settings);

            ILogQueryServiceFactory? logQueryServiceFactory =
                settings.LogQuery?.Enabled == true ? CreateLogQueryServiceFactory(settings.LogQuery) : null;

            var logger = new LoggerWrapper<RequestToSlackFunction>(log);
            return new RequestToSlackFunction(
                new MySummarizedAlertFactory(() => new MyDemuxedAlertHandler(logQueryServiceFactory)),
                new SlackClient(SlackClient.Configure(HttpClientFactory.Create()), settings.Slack ?? new SlackSettings()), // new SlackSettings { DefaultWebhook = GetSetting("slackWebhook") }),
                new MySlackMessageFactory(),
                settings.DebugSettings,
                logger);
        }

        public static JObject FlatDictionaryToJson(IDictionary dict, bool skipRoot = true)
        {
            var root = new JObject();
            foreach (var key in dict.Keys)
            {
                var k = key.ToString();
                var path = k!.Split(":");

                var numToSkip = skipRoot ? 1 : 0;
                var segments = path.Skip(numToSkip).Take(path.Length - 1 - numToSkip);
                var parent = segments.Aggregate(root, (c, p) =>
                {
                    var child = c[p] as JObject;
                    if (child == null)
                    {
                        child = new JObject();
                        c.Add(new JProperty(p, child));
                    }
                    return child;
                }) ?? root;

                parent.Add(new JProperty(path.Last(), dict[key]!.ToString()));
            }
            return root;
        }

        //private static void FillWithSettings(object obj)
        //{
        //    Recurse(obj, new Stack<string>());

        //    void Recurse(object parent, Stack<string> path)
        //    {
        //        var type = parent.GetType();
        //        var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        //        foreach (var prop in props)
        //        {
        //            path.Push(prop.Name);

        //            //if (prop.PropertyType.IsClass && prop.P)
        //            var fullname = string.Join(":", path);
        //            prop.SetValue(obj, GetSetting(fullname));

        //            path.Pop();
        //        }
        //    }
        //}

        private static string GetSetting(string key, string defaultValue = "")
        {
            var value = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
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

        private static ILogQueryServiceFactory CreateLogQueryServiceFactory(LogQuerySettings logQuerySettings)
        {
            //var logQuerySettings = new LogQuerySettings
            //{
            //    ApplicationInsights = new ApplicationInsightsQuerySettings
            //    {
            //        AppId = GetSetting("ApplicationInsightsAppId"),
            //        ApiKey = GetSetting("ApplicationInsightsApiKey")
            //    },
            //    Enabled = true,
            //    Timeout = 20,
            //    LogAnalytics = new LogAnalyticsQuerySettings
            //    {
            //        WorkspaceId = GetSetting("workspaceId")
            //    }
            //};
            var la = new LogAnalyticsQueryService(logQuerySettings.LogAnalytics);
            var ai = new AppInsightsQueryService(
                        new AppInsightsQueryService.ApplicationInsightsClient(
                            AppInsightsQueryService.ApplicationInsightsClient.ConfigureClient(HttpClientFactory.Create(), logQuerySettings.ApplicationInsights)));

            return new LogQueryServiceFactory(logQuerySettings, la, ai);
        }
    }
}
