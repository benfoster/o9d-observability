using Prometheus;

namespace O9d.Metrics.AspNet
{
    internal class PrometheusMetrics : IMetrics
    {
        private readonly IMetricFactory _factory 
            = Prometheus.Metrics.WithCustomRegistry(Prometheus.Metrics.DefaultRegistry);
        
        public ICollector<ICounter> CreateCounter(string name, string help, CounterConfiguration? configuration = null)
            => _factory.CreateCounter(name, help, configuration);

        public ICollector<IGauge> CreateGauge(string name, string help, GaugeConfiguration? configuration = null)
            => _factory.CreateGauge(name, help, configuration);

        public ICollector<IHistogram> CreateHistogram(string name, string help, HistogramConfiguration? configuration = null)
            => _factory.CreateHistogram(name, help, configuration);

        public ICollector<ISummary> CreateSummary(string name, string help, SummaryConfiguration? configuration = null)
            => _factory.CreateSummary(name, help, configuration);
    }
}