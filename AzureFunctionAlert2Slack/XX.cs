using AzureMonitorAlertToSlack;
using AzureMonitorAlertToSlack.Alerts;
using AzureMonitorAlertToSlack.LogQuery;
using AzureMonitorAlertToSlack.Slack;

namespace AzureFunctionAlert2Slack
{
    internal class MyDemuxedAlertHandler : DemuxedAlertHandler<SummarizedAlert, SummarizedAlertPart>
    {
        public MyDemuxedAlertHandler(ILogQueryServiceFactory? logQueryServiceFactory)
            : base(logQueryServiceFactory)
        { }
    }

    internal class MySlackMessageFactory : SlackMessageFactory<SummarizedAlert, SummarizedAlertPart>
    {
    }

    internal class MyMessageSender : SlackMessageSender<SummarizedAlert, SummarizedAlertPart>
    {
        public MyMessageSender(ISlackClient sender, MySlackMessageFactory messageFactory) : base(sender, messageFactory)
        { }
    }


    internal class MySummarizedAlertFactory : SummarizedAlertFactory<SummarizedAlert, SummarizedAlertPart>
    {
        public MySummarizedAlertFactory(IDemuxedAlertHandler<SummarizedAlert, SummarizedAlertPart> demuxedHandler)
            : base(demuxedHandler)
        { }
    }
}
