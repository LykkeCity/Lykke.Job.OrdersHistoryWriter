using Autofac;
using Lykke.Job.OrdersHistoryWriter.AzureRepositories;
using Lykke.Job.OrdersHistoryWriter.Core.Repositories;
using Lykke.Job.OrdersHistoryWriter.Core.Services;
using Lykke.Job.OrdersHistoryWriter.Services;
using Lykke.Job.OrdersHistoryWriter.Settings.JobSettings;
using Lykke.Job.OrdersHistoryWriter.RabbitSubscribers;

namespace Lykke.Job.OrdersHistoryWriter.Modules
{
    public class JobModule : Module
    {
        private readonly OrdersHistoryWriterSettings _settings;

        public JobModule(OrdersHistoryWriterSettings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .AutoActivate()
                .SingleInstance();

            builder.RegisterType<TradesRepository>()
                .As<ITradesRepository>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.Db.TradesConnString))
                .WithParameter("warningPartitionsCount", _settings.WarningPartitionsCount)
                .WithParameter("warningPartitionQueueCount", _settings.WarningPartitionQueueCount);

            builder.RegisterType<MarketOrdersRepository>()
                .As<IMarketOrdersRepository>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.Db.OrdersConnString))
                .WithParameter("warningPartitionsCount", _settings.WarningPartitionsCount)
                .WithParameter("warningPartitionQueueCount", _settings.WarningPartitionQueueCount);

            builder.RegisterType<LimitOrdersRepository>()
                .As<ILimitOrdersRepository>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(_settings.Db.OrdersConnString))
                .WithParameter("warningPartitionsCount", _settings.WarningPartitionsCount)
                .WithParameter("warningPartitionQueueCount", _settings.WarningPartitionQueueCount);

            builder.RegisterType<RabbitSubscriber>()
                .As<IStartStop>()
                .SingleInstance()
                .WithParameter("connectionString", _settings.Rabbit.ConnectionString)
                .WithParameter("exchangeName", _settings.Rabbit.ExchangeName);
        }
    }
}
