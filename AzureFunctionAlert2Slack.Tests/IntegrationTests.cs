using AzureMonitorCommonAlertSchemaTypes.AlertContexts.LogAlertsV2;
using AzureMonitorCommonAlertSchemaTypes.AlertContexts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AzureMonitorCommonAlertSchemaTypes;
using Shouldly;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctionAlert2Slack.Tests
{
    public class IntegrationTests
    {
        public IntegrationTests()
        {

        }

        [SkippableFact]
        public async Task Integration()
        {
            Skip.IfNot(System.Diagnostics.Debugger.IsAttached);

            var config = new ConfigurationBuilder()
                .AddUserSecrets(this.GetType().Assembly, optional: false)
                .Build();

            var services = new ServiceCollection();
            Startup.ConfigureServices(services, config, "TEST");

            var sp = services.BuildServiceProvider();


            var conditions = new[]
            {
                new LogQueryCriteria
                {
                    TargetResourceTypes = "['Microsoft.OperationalInsights/workspaces']",
                    Operator = "GreaterThan",
                    Threshold = 0,
                    TimeAggregation = "Count",
                    SearchQuery = @"AppTrace
| where SeverityLevel >= 3 and TimeGenerated < todatetime(""2023-01-28 14:25"")",
                }
            };
            var ctx = new LogAlertsV2AlertContext
            {
                ConditionType = conditions.First().ConditionTypeMatch.First(),
                Condition = new Condition
                {
                    WindowStartTime = DateTime.Now,
                    WindowEndTime = DateTime.Now,
                    AllOf = conditions
                },
            };

            var alert = CreateAlert(ctx);
            //var des = JsonConvert.SerializeObject(alert, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            //var oppo = AlertJsonSerializerSettings.DeserializeOrThrow(des);

            var function = sp.CreateInstance<RequestToSlackFunction>();
            var request = await Helpers.CreatePostJsonRequest(alert);
            
            var result = await function.Run(request);
            result.ShouldBeAssignableTo<OkObjectResult>();
        }

        private static Alert CreateAlert(IAlertContext ctx)
        {
            var alert = new Alert
            {
                SchemaId = "azureMonitorCommonAlertSchema",
                Data = new Data
                {
                    AlertContext = ctx,
                    Essentials = new Essentials
                    {
                        MonitoringService = ctx.MonitoringServiceMatches.First(),
                        //SignalType = "Log", // Metric
                        //EssentialsVersion = "1.0"
                    },
                }
            };
            return alert;
        }
    }

    public static class IServiceProviderExtensions
    {
        public static T CreateInstance<T>(this IServiceProvider instance) where T : class
        {
            var constructors = typeof(T).GetConstructors();

            var constructor = constructors.First();
            var parameterInfo = constructor.GetParameters();

            var parameters = parameterInfo.Select(o => instance.GetRequiredService(o.ParameterType)).ToArray();

            return (T)constructor.Invoke(parameters);
        }
    }

}
