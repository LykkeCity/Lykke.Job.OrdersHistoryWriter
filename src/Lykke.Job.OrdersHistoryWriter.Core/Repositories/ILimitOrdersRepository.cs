using System.Threading.Tasks;
using Lykke.Job.OrdersHistoryWriter.Core.Services;
using Lykke.MatchingEngine.Connector.Models.Events;

namespace Lykke.Job.OrdersHistoryWriter.Core.Repositories
{
    public interface ILimitOrdersRepository : IStartStop
    {
        Task RegisterAsync(Order limitOrder);
    }
}
