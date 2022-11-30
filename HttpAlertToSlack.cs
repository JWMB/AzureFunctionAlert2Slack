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

        [FunctionName("HttpAlertToSlack")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            ILogQueryServiceFactory? logQueryServiceFactory = 
                Environment.GetEnvironmentVariable("UseLogQueryService") == "1" 
                ? CreateLogQueryServiceFactory()
                : null;

            return await RunInternal(req, 
                new AlertInfoFactory(new DemuxedAlertInfoHandler(logQueryServiceFactory)),
                new SlackMessageSender(new SlackClient(SlackClient.Configure(HttpClientFactory.Create())), new SlackMessageFactory()), log);
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


        private static async Task<IActionResult> RunInternal(HttpRequest req, IAlertInfoFactory alertInfoFactory, IMessageSender sender, ILogger log)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //log.LogInformation(requestBody);

            if (requestBody == null)
            {
                return new BadRequestObjectResult($"Body was null");
            }

            List<AlertInfo> items;
            Exception? parseException = null;
            try
            {
                items = await alertInfoFactory.Process(requestBody);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                // Don't throw immediately - let this error message be sent first
                parseException = ex;
                items = new List<AlertInfo>{
                    new AlertInfo{ Title = "Unknown alert", Text = ex.Message },
                    new AlertInfo{ Title = "Body", Text = requestBody }
                };
            }

            if (Environment.GetEnvironmentVariable("DebugPayload") == "1") // TODO: change when DI problem solved
            {
                items.Last().Text += $"\\n{requestBody}";
            }

            try
            {
                await sender.SendMessage(items);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

                if (ex.Message.Contains("invalid_attachments"))
                {
                    // TODO: how can we validate slack content?
                    // One problem is escape chars, like <>.
                    // Maybe we should provide a HTML document instead and render it to mrkdwn
                    try
                    {
                        await sender.SendMessage(new[] { new AlertInfo { Title = "Slack error response", Text = ex.Message } });
                    }
                    catch { }
                }

                return new BadRequestObjectResult($"Failed to send message: {ex.Message} ({ex.GetType().Name})"); // TODO: some other response type
            }

            return parseException != null
                ? new BadRequestObjectResult($"Could not read body: {parseException.Message}")
                : new OkObjectResult("");
        }

    }
}
