using AzureMonitorAlertToSlack;
using AzureMonitorAlertToSlack.Alerts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureFunctionAlert2Slack
{
    public class RequestToSlackFunction
    {
        private readonly IAlertInfoFactory alertInfoFactory;
        private readonly IMessageSender sender;
        private readonly ILogger<RequestToSlackFunction> log;

        public RequestToSlackFunction(IAlertInfoFactory alertInfoFactory, IMessageSender sender, ILogger<RequestToSlackFunction> log)
        {
            this.alertInfoFactory = alertInfoFactory;
            this.sender = sender;
            this.log = log;
        }

        public async Task<IActionResult> Run(HttpRequest req)
        {
            if (req.Body.Length == 0)
                return new BadRequestObjectResult($"Body was null");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //log.LogInformation(requestBody);

            if (string.IsNullOrEmpty(requestBody))
                return new BadRequestObjectResult($"Body was {(requestBody == null ? "null" : "empty")}");

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
