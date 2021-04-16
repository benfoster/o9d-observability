using System;
using Microsoft.Extensions.DependencyInjection;

namespace O9d.Observability
{
    /// <summary>
    /// Extensibility point for adding observability components.
    /// </summary>
    public interface IObservabilityBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> where services are configured.
        /// /// </summary>
        IServiceCollection Services { get; }
        
        /// <summary>
        /// Add an instrumentation component to the builder.
        /// </summary>
        /// <param name="instrumentationFactory">A factory used to construct the instrumentation component.</param>
        /// <returns></returns>
        IObservabilityBuilder AddInstrumentation(Func<IServiceProvider, IInstrumentation> instrumentationFactory);
    }
}