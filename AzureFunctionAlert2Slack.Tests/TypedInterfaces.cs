using AzureMonitorAlertToSlack.Alerts;
using AzureMonitorAlertToSlack;

namespace AzureFunctionAlert2Slack.Tests
{
    public interface IMessageSenderTyped : IMessageSender<SummarizedAlert, SummarizedAlertPart>
    {
    }
    public interface ISummarizedAlertFactoryTyped : ISummarizedAlertFactory<SummarizedAlert, SummarizedAlertPart>
    {
    }
}
