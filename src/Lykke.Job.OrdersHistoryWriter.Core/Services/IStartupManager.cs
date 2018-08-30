using System.Threading.Tasks;

namespace Lykke.Job.OrdersHistoryWriter.Core.Services
{
    public interface IStartupManager
    {
        Task StartAsync();
    }
}