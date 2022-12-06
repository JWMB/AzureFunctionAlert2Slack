using AzureMonitorAlertToSlack.Alerts;
using AzureMonitorAlertToSlack.Slack;
using System;

namespace AzureFunctionAlert2Slack
{
    internal class MySlackMessageFactory : SlackMessageFactory<SummarizedAlert, SummarizedAlertPart>
    {
    }

    internal class MySummarizedAlertFactory : SummarizedAlertFactory<SummarizedAlert, SummarizedAlertPart>
    {
        public MySummarizedAlertFactory(Func<IDemuxedAlertHandler<SummarizedAlert, SummarizedAlertPart>> demuxedHandler)
            : base(demuxedHandler)
        { }
    }
}
