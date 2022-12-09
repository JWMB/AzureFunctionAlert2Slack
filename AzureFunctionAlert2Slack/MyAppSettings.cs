using AzureMonitorAlertToSlack;

namespace AzureFunctionAlert2Slack
{
    public class MyAppSettings : AppSettings
    {
        public DebugSettings DebugSettings { get; set; } = new();
    }

    public class DebugSettings
    {
        public bool AddPayloadToMessage { get; set; }
    }
}
