using System.Threading;
using System.Threading.Tasks;

namespace O9d.Observability
{
    /// <summary>
    /// Abstraction for observability hosts that bootstrap observability components
    /// </summary>
    public interface IObservabilityHost
    {
        /// <summary>
        /// Starts the observability host
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StartAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// Stops the observability host
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StopAsync(CancellationToken cancellationToken);
    }
}