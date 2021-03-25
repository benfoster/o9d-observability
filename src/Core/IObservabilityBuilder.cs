using System;
using Microsoft.Extensions.DependencyInjection;

namespace O9d.Observability
{
    /// <summary>
    /// Extensibility point for adding observability components
    /// </summary>
    public interface IObservabilityBuilder
    {
        /// <summary>
        /// Add an instrumentation component to the builder
        /// </summary>
        /// <param name="instrumentationFactory">A factory used to construct the instrumentation component</param>
        /// <returns></returns>
        IObservabilityBuilder AddInstrumentation(Func<IServiceProvider, IInstrumentation> instrumentationFactory);
        
        /// <summary>
        /// Gets the service collection used to construct the application service provider
        /// </summary>
        IServiceCollection Services { get; }
    }
}