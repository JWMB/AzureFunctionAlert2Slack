using AzureMonitorAlertToSlack.Alerts;
using AzureMonitorAlertToSlack;
using AzureMonitorAlertToSlack.Slack;

namespace AzureFunctionAlert2Slack.Tests
{
    public interface IMessageSenderTyped : IMessageSender<SummarizedAlert, SummarizedAlertPart>
    {
    }
    public interface ISlackMessageFactoryTyped : ISlackMessageFactory<SummarizedAlert, SummarizedAlertPart>
    {
    }

    public interface ISummarizedAlertFactoryTyped : ISummarizedAlertFactory<SummarizedAlert, SummarizedAlertPart>
    {
    }
}
