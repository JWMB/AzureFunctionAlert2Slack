using AzureMonitorAlertToSlack.Alerts;
using AzureMonitorAlertToSlack.LogQuery;
using AzureMonitorCommonAlertSchemaTypes;
using AzureMonitorCommonAlertSchemaTypes.AlertContexts;
using AzureMonitorCommonAlertSchemaTypes.AlertContexts.LogAlertsV2;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureFunctionAlert2Slack
{
    public class MyDemuxedAlertHandler : DemuxedAlertHandler<SummarizedAlert, SummarizedAlertPart>
    {
        public MyDemuxedAlertHandler(ILogQueryServiceFactory? logQueryServiceFactory)
            : base(logQueryServiceFactory)
        { }

        protected override void SetHandled(SummarizedAlert item)
        {
            item.Title = $"{GetCustomProperty(item, "titlePrefix")}{item.Title}{GetCustomProperty(item, "titleSuffix")}";
            base.SetHandled(item);
        }

        protected override SummarizedAlert CreateBasic(Alert alert, string? createPartWithText)
        {
            var item = base.CreateBasic(alert, createPartWithText);

            // TODO: we should add an overridable CreatePart method
            foreach (var part in item.Parts)
                SetColor(part, alert);

            return item;
        }

        public override void LogAlertsV2AlertContext(Alert alert, LogAlertsV2AlertContext ctx, DynamicThresholdCriteria[] criteria)
        {
            base.LogAlertsV2AlertContext(alert, ctx, criteria);
        }

        protected override SummarizedAlertPart CreatePartFromV2ConditionPart(Alert alert, LogAlertsV2AlertContext ctx, IConditionPart? conditionPart)
        {
            SummarizedAlertPart part;
            if (conditionPart is LogQueryCriteria lq)
            {
                part = new SummarizedAlertPart();
                part.Text = $"{lq.MetricValue}{lq.MetricValue}{lq.OperatorToken}{lq.Threshold}\nQuery:{lq.SearchQuery.Truncate(100)}";
            }
            else
                part = base.CreatePartFromV2ConditionPart (alert, ctx, conditionPart);

            SetColor(part, alert);

            return part;
        }

        protected override Task<string?> QueryAI(SummarizedAlert handled, string targetResourceTypes, string? query, DateTimeOffset start, DateTimeOffset end)
        {
            var querySuffix = GetCustomProperty(handled, "querySuffix");
            query = $"{query}{(string.IsNullOrEmpty(querySuffix) ? "" : $"\n{querySuffix}")}";
            return base.QueryAI(handled, targetResourceTypes, query, start, end);
        }

        private string GetCustomProperty(SummarizedAlert item, string key, string defaultValue = "") =>
                item.CustomProperties?.GetValueOrDefault(key, defaultValue) ?? defaultValue;

        private void SetColor(SummarizedAlertPart part, Alert alert)
        {
            var severity = alert.Data.Essentials.Severity?.ToLower();
            switch (severity)
            {
                case "sev1":
                case "information":
                    part.Color = "#0872c4";
                    break;
                case "sev2":
                case "warning":
                    part.Color = "#fbd023";
                    break;
                case "sev3":
                case "error":
                    part.Color = "#dc5805";
                    break;
                case "sev4":
                case "critical":
                    part.Color = "#e71123";
                    break;
            }
        }
    }
}
