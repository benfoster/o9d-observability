using Prometheus;

namespace O9d.Metrics.AspNet
{
    public interface IMetrics
    {
        ICollector<ICounter> CreateCounter(string name, string help, CounterConfiguration? configuration = null);
        ICollector<IGauge> CreateGauge(string name, string help, GaugeConfiguration? configuration = null);
        ICollector<IHistogram> CreateHistogram(string name, string help, HistogramConfiguration? configuration = null);
        ICollector<ISummary> CreateSummary(string name, string help, SummaryConfiguration? configuration = null);
    }
}