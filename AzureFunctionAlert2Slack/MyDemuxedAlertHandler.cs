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
        public override void LogAlertsV2AlertContext(Alert alert, LogAlertsV2AlertContext ctx, DynamicThresholdCriteria[] criteria)
        {
            base.LogAlertsV2AlertContext(alert, ctx, criteria);
        }

        protected override Task<string?> QueryAI(SummarizedAlert handled, string targetResourceTypes, string? query, DateTimeOffset start, DateTimeOffset end)
        {
            var querySuffix = GetCustomProperty(handled, "querySuffix");
            query = $"{query}{(string.IsNullOrEmpty(querySuffix) ? "" : $"\n{querySuffix}")}";
            return base.QueryAI(handled, targetResourceTypes, query, start, end);
        }

        private string GetCustomProperty(SummarizedAlert item, string key, string defaultValue = "") =>
                item.CustomProperties?.GetValueOrDefault(key, defaultValue) ?? defaultValue;

    }
}
