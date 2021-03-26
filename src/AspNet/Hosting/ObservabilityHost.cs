using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace O9d.Observability.AspNet.Hosting
{
    /// <summary>
    /// Host for observability components
    /// </summary>
    internal class ObservabilityHost : IHostedService
    {
        private readonly IEnumerable<IInstrumentation> _instrumentations;

        public ObservabilityHost(IEnumerable<IInstrumentation> instrumentations)
        {
            _instrumentations = instrumentations ?? throw new ArgumentNullException(nameof(instrumentations));
        }

        ///<inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (IInstrumentation instrumentation in _instrumentations)
            {
                await instrumentation.StartAsync(cancellationToken);
            }
        }

        ///<inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (IInstrumentation instrumentation in _instrumentations)
            {
                (instrumentation as IDisposable)?.Dispose();
            }

            return Task.CompletedTask;
        }
    }
}