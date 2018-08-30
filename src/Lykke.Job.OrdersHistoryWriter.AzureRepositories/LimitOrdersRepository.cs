using System.Threading.Tasks;
using Lykke.Common.Log;
using Lykke.Job.OrdersHistoryWriter.Core.Repositories;
using Lykke.MatchingEngine.Connector.Models.Events;

namespace Lykke.Job.OrdersHistoryWriter.AzureRepositories
{
    public class LimitOrdersRepository : ILimitOrdersRepository
    {
        private readonly ITradesRepository _tradesRepository;
        private readonly BatchSaver<LimitOrderEntity> _batchSaver;

        public LimitOrdersRepository(
            ILogFactory logFactory,
            ITradesRepository tradesRepository,
            string connectionString)
        {
            _tradesRepository = tradesRepository;
            _batchSaver = new BatchSaver<LimitOrderEntity>(
                connectionString,
                "LimitOrders",
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

        public async Task RegisterAsync(Order limitOrder)
        {
            var byClient = LimitOrderEntity.ByClientId.FromMeModel(limitOrder);
            var byDate = LimitOrderEntity.ByDate.FromMeModel(limitOrder);

            await _batchSaver.AddAsync(byClient, byDate);

            if (limitOrder.Trades != null)
                await _tradesRepository.RegisterAsync(
                    limitOrder.Trades,
                    limitOrder.WalletId,
                    limitOrder.ExternalId ?? limitOrder.Id,
                    true);
        }
    }
}
