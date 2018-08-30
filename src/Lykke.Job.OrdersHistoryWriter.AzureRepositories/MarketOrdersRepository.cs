using System.Threading.Tasks;
using Lykke.Common.Log;
using Lykke.Job.OrdersHistoryWriter.Core.Repositories;
using Lykke.MatchingEngine.Connector.Models.Events;

namespace Lykke.Job.OrdersHistoryWriter.AzureRepositories
{
    public class MarketOrdersRepository : IMarketOrdersRepository
    {
        private readonly ITradesRepository _tradesRepository;
        private readonly BatchSaver<MarketOrderEntity> _batchSaver;

        public MarketOrdersRepository(
            ILogFactory logFactory,
            ITradesRepository tradesRepository,
            string connectionString)
        {
            _tradesRepository = tradesRepository;
            _batchSaver = new BatchSaver<MarketOrderEntity>(
                connectionString,
                "MarketOrders",
                logFactory);
        }

        public void Start()
        {
            _batchSaver.Start();
        }

        public void Dispose()
        {
            Stop();

            _batchSaver.Dispose();
        }

        public void Stop()
        {
            _batchSaver.Stop();
        }

        public async Task RegisterAsync(Order marketOrder)
        {
            var byClient = MarketOrderEntity.ByClientId.FromMeModel(marketOrder);
            var byDate = MarketOrderEntity.ByDate.FromMeModel(marketOrder);

            await _batchSaver.AddAsync(byClient, byDate);

            if (marketOrder.Trades != null)
                await _tradesRepository.RegisterAsync(
                    marketOrder.Trades,
                    marketOrder.WalletId,
                    marketOrder.ExternalId ?? marketOrder.Id,
                    false);
        }
    }
}
