using Autofac;
using Common;

namespace Lykke.Job.OrdersHistoryWriter.Core.Services
{
    public interface IStartStop : IStartable, IStopable
    {
    }
}
