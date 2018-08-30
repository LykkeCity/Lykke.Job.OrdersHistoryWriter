using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.OrdersHistoryWriter.Settings.JobSettings
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }

        [AzureTableCheck]
        public string OrdersConnString { get; set; }

        [AzureTableCheck]
        public string TradesConnString { get; set; }
    }
}
