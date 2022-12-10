using AzureMonitorAlertToSlack.Alerts;
using AzureMonitorAlertToSlack.LogQuery;
using AzureMonitorCommonAlertSchemaTypes;
using AzureMonitorCommonAlertSchemaTypes.AlertContexts;
using AzureMonitorCommonAlertSchemaTypes.AlertContexts.LogAlertsV2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureFunctionAlert2Slack
{
    public class MyDemuxedAlertHandler : DemuxedAlertHandler<SummarizedAlert, SummarizedAlertPart>
    {
        public MyDemuxedAlertHandler(ILogQueryServiceFactory? logQueryServiceFactory)
            : base(logQueryServiceFactory)
        { }

        private Alert? currentlyProcessedAlert;

        protected override void CreateResult(Alert alert, string? createPartWithText)
        {
            currentlyProcessedAlert = alert;
            base.CreateResult(alert, createPartWithText);
        }

        protected override void PostProcess()
        {
            if (Result != null)
                // TODO: CustomProperties are not what I thought
                Result.Title = $"{GetCustomProperty(Result, "titlePrefix")}{Result.Title}{GetCustomProperty(Result, "titleSuffix")}";
        }

        protected override SummarizedAlertPart CreatePart()
        {
            var part = base.CreatePart();
            if (currentlyProcessedAlert != null)
                SetColor(part, currentlyProcessedAlert);
            return part;
        }

        public override void LogAlertsV2AlertContext(Alert alert, LogAlertsV2AlertContext ctx)
        {
            base.LogAlertsV2AlertContext(alert, ctx);

            // Copy ctx.Properties to Result (sure, we override the CustomProperties, but can't see how those are populated anyway..?)
            Result.CustomProperties ??= new Dictionary<string, string>();
            foreach (var kv in ctx.Properties)
                Result.CustomProperties.Add(kv.Key, kv.Value);
        }

        protected override SummarizedAlertPart CreatePartFromV2ConditionPart(Alert alert, LogAlertsV2AlertContext ctx, IConditionPart? conditionPart)
        {
            SummarizedAlertPart part;
            if (conditionPart is LogQueryCriteria lq)
            {
                part = CreatePart();
                part.Text = $"{lq.MetricMeasureColumn}: {lq.MetricValue} {lq.OperatorToken} {lq.Threshold} ({ctx.Condition.GetUserFriendlyTimeWindowString()})\nQuery:{lq.SearchQuery.Truncate(100)}";
                if (ctx.Properties.TryGetValue("querySuffix", out var querySuffix))
                    // TODO: ugly to modify the actual property...
                    lq.SearchQuery = $"{lq.SearchQuery.Trim()}{(string.IsNullOrEmpty(querySuffix) ? "" : $"\n{querySuffix}")}";
            }
            else
                part = base.CreatePartFromV2ConditionPart (alert, ctx, conditionPart);

            return part;
        }

        private string GetCustomProperty(SummarizedAlert item, string key, string defaultValue = "") =>
                item.CustomProperties?.GetValueOrDefault(key, defaultValue) ?? defaultValue;

        private void SetColor(SummarizedAlertPart part, Alert alert)
        {
            var severity = alert.Data.Essentials.Severity?.ToLower();
            switch (severity)
            {
                case "sev0":
                case "critical":
                    part.Color = "#e71123";
                    break;
                case "sev1":
                case "error":
                    part.Color = "#dc5805";
                    break;
                case "sev2":
                case "warning":
                    part.Color = "#fbd023";
                    break;
                case "sev3":
                case "information":
                    part.Color = "#0872c4";
                    break;
                case "sev4":
                case "verbose":
                    part.Color = "#05198d";
                    break;
            }
        }
    }
}
