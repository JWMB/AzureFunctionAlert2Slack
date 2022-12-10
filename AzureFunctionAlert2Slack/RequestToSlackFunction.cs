using AzureMonitorAlertToSlack;
using AzureMonitorAlertToSlack.Alerts;
using AzureMonitorAlertToSlack.Slack;
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
        private readonly ISummarizedAlertFactory<SummarizedAlert, SummarizedAlertPart> alertInfoFactory;
        private readonly ISlackClient slackClient;
        private readonly ISlackMessageFactory<SummarizedAlert, SummarizedAlertPart> messageFactory;
        private readonly DebugSettings debugSettings;
        private readonly ILogger<RequestToSlackFunction> log;

        public RequestToSlackFunction(ISummarizedAlertFactory<SummarizedAlert, SummarizedAlertPart> alertInfoFactory,
            ISlackClient slackClient, ISlackMessageFactory<SummarizedAlert, SummarizedAlertPart> messageFactory,
            DebugSettings debugSettings,
            ILogger<RequestToSlackFunction> log)
        {
            this.alertInfoFactory = alertInfoFactory;
            this.slackClient = slackClient;
            this.messageFactory = messageFactory;
            this.debugSettings = debugSettings;
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

            SummarizedAlert summary;
            Exception? parseException = null;
            try
            {
                summary = await alertInfoFactory.Process(requestBody);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);

                // Don't throw immediately - let this error message be sent first
                parseException = ex;
                summary = new SummarizedAlert
                {
                    Parts = new List<SummarizedAlertPart>
                    {
                        new SummarizedAlertPart{ Title = "Unknown alert", Text = ex.ToStringRecursive(3000) },
                        new SummarizedAlertPart{ Title = "Body", Text = requestBody }
                    }
                };
            }

            if (summary.Parts.Any() == false)
            {
                summary.Parts.Add(new SummarizedAlertPart { Text = "No parts created" });
            }

            if (debugSettings.AddPayloadToMessage)
            {
                summary.Parts.Last().Text += $"\\n{requestBody}";
            }

            try
            {
                await SendMessage(summary);
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
                        await SendMessage(new SummarizedAlert { Parts = new List<SummarizedAlertPart> { new SummarizedAlertPart { Title = "Slack error response", Text = ex.Message } } });
                    }
                    catch { }
                }

                return new BadRequestObjectResult($"Failed to send message: {ex.Message} ({ex.GetType().Name})"); // TODO: some other response type
            }

            return parseException != null
                ? new BadRequestObjectResult($"Could not read body: {parseException.Message}")
                : new OkObjectResult("");
        }

        private async Task SendMessage(SummarizedAlert summary)
        {
            var slackBody = messageFactory.CreateMessage(summary);
            string? webhook = null;
            summary.CustomProperties?.TryGetValue("slackWebhook", out webhook);
            await slackClient.Send(slackBody, webhook);
        }
    }
}
