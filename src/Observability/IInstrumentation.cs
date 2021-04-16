using System.Threading;
using System.Threading.Tasks;

namespace O9d.Observability
{
    /// <summary>
    /// Defines an instrumentation component that can be initialised by the Observability Host.
    /// </summary>
    public interface IInstrumentation
    {
        /// <summary>
        /// Start instrumenting.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StartAsync(CancellationToken cancellationToken);
    }
}