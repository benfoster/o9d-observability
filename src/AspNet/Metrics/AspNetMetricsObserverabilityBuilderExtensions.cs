using System;
using O9d.Observability.AspNet.Metrics;

namespace O9d.Observability
{
    public static class AspNetMetricsObservabilityBuilderExtensions
    {
        public static IObservabilityBuilder AddAspNetMetrics(this IObservabilityBuilder builder, Action<AspNetMetricsOptions>? configureOptions = null)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            var options = new AspNetMetricsOptions();
            configureOptions?.Invoke(options);

            return builder.AddDiagnosticSource("Microsoft.AspNetCore", new AspNetMetricsObserver(options));
        }
    }
}