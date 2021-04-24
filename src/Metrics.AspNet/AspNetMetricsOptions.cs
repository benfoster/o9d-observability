using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Prometheus;

namespace O9d.Metrics.AspNet
{
    public class AspNetMetricsOptions
    {
        public AspNetMetricsOptions()
        {
            ShouldInstrument = path => !path.StartsWithSegments("/metrics");
            RequestDurationMetricType = ObserverMetricType.Histogram;
        }

        /// <summary>
        /// Gets or sets the criteria for whether the path should be instrumented
        /// </summary>
        public Func<PathString, bool> ShouldInstrument { get; set; }

        /// <summary>
        /// Gets or sets the configuration action applied to the error total counter
        /// </summary>
        public Action<CounterConfiguration>? ConfigureErrorTotalCounter { get; set; }

        /// <summary>
        /// Gets or sets the configuration action applied to the requests in progress gauge
        /// </summary>
        public Action<GaugeConfiguration>? ConfigureRequestsInProgressGauge { get; set; }

        /// <summary>
        /// Gets or sets the type of observer used for the request duration metric
        /// </summary>
        public ObserverMetricType RequestDurationMetricType { get; set; }

        /// <summary>
        /// Gets or sets the configuration action applied to the request duration histogram
        /// </summary>
        public Action<HistogramConfiguration>? ConfigureRequestDurationHistogram { get; set; }

        /// <summary>
        /// Gets or sets the configuration action applied to the request duration summary
        /// </summary>
        public Action<SummaryConfiguration>? ConfigureRequestDurationSummary { get; set; }

        /// <summary>
        /// Gets or sets the contextual labels used as a source to metrics
        /// </summary>
        public Dictionary<string, Func<HttpContext, string>>? ContextualLabels { get; set; }
    }
}