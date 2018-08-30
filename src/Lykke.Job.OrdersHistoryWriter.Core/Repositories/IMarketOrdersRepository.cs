using Lykke.Job.OrdersHistoryWriter.Core.Services;
using Lykke.MatchingEngine.Connector.Models.Events;
using System.Threading.Tasks;

namespace Lykke.Job.OrdersHistoryWriter.Core.Repositories
{
    public interface IMarketOrdersRepository : IStartStop
    {
        Task RegisterAsync(Order marketOrder);
    }
}
