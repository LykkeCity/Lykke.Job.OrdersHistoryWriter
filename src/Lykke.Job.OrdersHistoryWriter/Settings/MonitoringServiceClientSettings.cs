using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.OrdersHistoryWriter.Settings
{
    public class MonitoringServiceClientSettings
    {
        [HttpCheck("api/isalive")]
        public string MonitoringServiceUrl { get; set; }
    }
}
