using Lykke.Job.OrdersHistoryWriter.Core.Services;
using Lykke.MatchingEngine.Connector.Models.Events;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.OrdersHistoryWriter.Core.Repositories
{
    public interface ITradesRepository : IStartStop
    {
        Task RegisterAsync(
            List<Trade> trades,
            string walletId,
            string orderId,
            bool isLimitOrder);
    }
}
