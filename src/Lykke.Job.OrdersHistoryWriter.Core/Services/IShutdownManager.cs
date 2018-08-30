using System.Threading.Tasks;

namespace Lykke.Job.OrdersHistoryWriter.Core.Services
{
    public interface IShutdownManager
    {
        Task StopAsync();
    }
}
