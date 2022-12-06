using AzureMonitorAlertToSlack.Alerts;
using AzureMonitorAlertToSlack.LogQuery;
using AzureMonitorCommonAlertSchemaTypes;
using AzureMonitorCommonAlertSchemaTypes.AlertContexts;
using AzureMonitorCommonAlertSchemaTypes.AlertContexts.LogAlertsV2;
using System.Collections.Generic;

namespace AzureFunctionAlert2Slack
{
    public class MyDemuxedAlertHandler : DemuxedAlertHandler<SummarizedAlert, SummarizedAlertPart>
    {
        public MyDemuxedAlertHandler(ILogQueryServiceFactory? logQueryServiceFactory)
            : base(logQueryServiceFactory)
        { }

        protected override void SetHandled(SummarizedAlert item)
        {
            item.Title = $"{GetCustomProperty("titlePrefix")}{item.Title}{GetCustomProperty("titleSuffix")}";
            base.SetHandled(item);

            string GetCustomProperty(string key, string defaultValue = "") =>
                item.CustomProperties?.GetValueOrDefault(key, defaultValue) ?? defaultValue;
        }
        public override void LogAlertsV2AlertContext(Alert alert, LogAlertsV2AlertContext ctx, DynamicThresholdCriteria[] criteria)
        {
            base.LogAlertsV2AlertContext(alert, ctx, criteria);
        }
    }
}
