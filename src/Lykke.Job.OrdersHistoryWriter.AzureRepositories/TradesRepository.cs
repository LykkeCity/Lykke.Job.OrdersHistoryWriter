using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Common.Log;
using Lykke.Job.OrdersHistoryWriter.Core.Repositories;
using Lykke.MatchingEngine.Connector.Models.Events;

namespace Lykke.Job.OrdersHistoryWriter.AzureRepositories
{
    public class TradesRepository : ITradesRepository
    {
        private readonly BatchSaver<TradeEntity> _batchSaver;

        public TradesRepository(ILogFactory logFactory, string connectionString)
        {
            _batchSaver = new BatchSaver<TradeEntity>(
                connectionString,
                "Trades",
                logFactory);
        }

        public void Start()
        {
            _batchSaver.Start();
        }

        public void Dispose()
        {
            _batchSaver.Dispose();
        }

        public void Stop()
        {
            _batchSaver.Stop();
        }

        public async Task RegisterAsync(
            List<Trade> trades,
            string walletId,
            string orderId,
            bool isLimitOrder)
        {
            foreach (var trade in trades)
            {
                var byClient = TradeEntity.ByClientId.FromMeModel(
                    trade,
                    walletId,
                    orderId,
                    isLimitOrder);
                var byDate = TradeEntity.ByDate.FromMeModel(
                    trade,
                    walletId,
                    orderId,
                    isLimitOrder);
                var byOrder = TradeEntity.ByOrder.FromMeModel(
                    trade,
                    walletId,
                    orderId,
                    isLimitOrder);

                await _batchSaver.AddAsync(byClient, byDate, byOrder);
            }
        }
    }
}
