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
                function = StartupFallback.CreateFunction(Environment.GetEnvironmentVariables(), log);
            }

            return await function.Run(req);
        }
    }
}
