namespace Lykke.Job.OrdersHistoryWriter.Settings.JobSettings
{
    public class OrdersHistoryWriterSettings
    {
        public DbSettings Db { get; set; }
        public RabbitMqSettings Rabbit { get; set; }
    }
}
