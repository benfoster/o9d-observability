using System;
using O9d.Observability;

namespace O9d.Metrics.AspNet
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