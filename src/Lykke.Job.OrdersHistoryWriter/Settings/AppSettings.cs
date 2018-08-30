using Lykke.Job.OrdersHistoryWriter.Settings.JobSettings;
using Lykke.Job.OrdersHistoryWriter.Settings.SlackNotifications;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.OrdersHistoryWriter.Settings
{
    public class AppSettings
    {
        public OrdersHistoryWriterSettings OrdersHistoryWriterJob { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }

        [Optional]
        public MonitoringServiceClientSettings MonitoringServiceClient { get; set; }
    }
}
