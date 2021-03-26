using System;
using Microsoft.AspNetCore.Http;

namespace O9d.Observability.AspNet.Metrics
{
    public class AspNetMetricsOptions
    {
        /// <summary>
        /// Gets or sets the criteria for whether the path should be instrumented
        /// </summary>
        public Func<PathString, bool> ShouldInstrument { get; set; } // TODO make sure this can't be null

        public AspNetMetricsOptions()
        {
            ShouldInstrument = path => !path.StartsWithSegments("/metrics");
        }
    }
}