using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.OrdersHistoryWriter.Core.Repositories;
using Lykke.Job.OrdersHistoryWriter.Core.Services;
using Lykke.MatchingEngine.Connector.Models.Events;
using Lykke.MatchingEngine.Connector.Models.Events.Common;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Job.OrdersHistoryWriter.RabbitSubscribers
{
    public class RabbitSubscriber : IStartStop
    {
        private readonly ILogFactory _logFactory;
        private readonly ITradesRepository _tradesRepository;
        private readonly IMarketOrdersRepository _marketOrdersRepository;
        private readonly ILimitOrdersRepository _limitOrdersRepository;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private RabbitMqSubscriber<ExecutionEvent> _subscriber;

        public RabbitSubscriber(
            ILogFactory logFactory,
            ITradesRepository tradesRepository,
            IMarketOrdersRepository marketOrdersRepository,
            ILimitOrdersRepository limitOrdersRepository,
            string connectionString,
            string exchangeName)
        {
            _logFactory = logFactory;
            _tradesRepository = tradesRepository;
            _marketOrdersRepository = marketOrdersRepository;
            _limitOrdersRepository = limitOrdersRepository;
            _connectionString = connectionString;
            _exchangeName = exchangeName;
        }

        public void Start()
        {
            _tradesRepository.Start();
            _marketOrdersRepository.Start();
            _limitOrdersRepository.Start();

            var settings = RabbitMqSubscriptionSettings
                .CreateForSubscriber(_connectionString, _exchangeName, "ordershistorywriter")
                .MakeDurable()
                .UseRoutingKey(((int) MessageType.Order).ToString());

            _subscriber = new RabbitMqSubscriber<ExecutionEvent>(
                    _logFactory,
                    settings,
                    new ResilientErrorHandlingStrategy(
                        _logFactory,
                        settings,
                        TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_logFactory, settings)))
                .SetMessageDeserializer(new ProtobufMessageDeserializer<ExecutionEvent>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .SetConsole(new LogToConsole())
                .Start();
        }

        private async Task ProcessMessageAsync(ExecutionEvent arg)
        {
            foreach (var order in arg.Orders)
            {
                switch (order.OrderType)
                {
                    case OrderType.Market:
                        await _marketOrdersRepository.RegisterAsync(order);
                        return;
                    case OrderType.Limit:
                        await _limitOrdersRepository.RegisterAsync(order);
                        return;
                    default:
                        //ignore
                        return;
                }
            }
        }

        public void Dispose()
        {
            Stop();

            _subscriber?.Dispose();
        }

        public void Stop()
        {
            _subscriber?.Stop();

            _limitOrdersRepository.Stop();
            _marketOrdersRepository.Stop();
            _tradesRepository.Stop();
        }
    }
}
