using System;
using O9d.Observability;
using O9d.Observability.AspNet;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for <see cref="IServiceCollection"/>
    /// </summary>
    public static class ObservabilityServiceCollectionExtensions
    {
        /// <summary>
        /// Registers observability components with the application service provider
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configure">Configuration expression for the observability builder</param>
        /// <returns>The service collection</returns>
        public static IObservabilityBuilder AddObservability(this IServiceCollection services, Action<IObservabilityBuilder>? configure = null)
        {
            var builder = new ObservabilityBuilder(services);
            configure?.Invoke(builder);

            builder.Build();
            return builder;
        }
    }
}