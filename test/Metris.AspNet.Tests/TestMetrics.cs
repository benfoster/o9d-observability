using System.Collections.Concurrent;
using Moq;
using Prometheus;

namespace O9d.Metrics.AspNet.Tests
{
    public class TestMetrics : IMetrics
    {
        private readonly ConcurrentDictionary<string, ICollector> _metrics = new();

        public ICollector<ICounter> CreateCounter(string name, string help, CounterConfiguration? configuration = null)
        {
            return (ICollector<ICounter>)_metrics.GetOrAdd(name, _ => CreateMock<ICounter>());
        }

        public ICollector<IGauge> CreateGauge(string name, string help, GaugeConfiguration? configuration = null)
        {
            return (ICollector<IGauge>)_metrics.GetOrAdd(name, _ => CreateMock<IGauge>());
        }

        public ICollector<IHistogram> CreateHistogram(string name, string help, HistogramConfiguration? configuration = null)
        {
            return (ICollector<IHistogram>)_metrics.GetOrAdd(name, _ => CreateMock<IHistogram>());
        }

        public ICollector<ISummary> CreateSummary(string name, string help, SummaryConfiguration? configuration = null)
        {
            return (ICollector<ISummary>)_metrics.GetOrAdd(name, _ => CreateMock<ISummary>());
        }

        public MockedMetric<TChild>? GetMetric<TChild>(string name) where TChild : class, ICollectorChild
        {
            if (_metrics.TryGetValue(name, out ICollector? value) && value is ICollector<TChild> metric)
            {
                return new MockedMetric<TChild>(Mock.Get(metric),Mock.Get(metric.Unlabelled));
            }

            return default;
        }

        private static ICollector<TChild> CreateMock<TChild>() where TChild : class, ICollectorChild
        {
            var child = new Mock<TChild>();
            var collector = new Mock<ICollector<TChild>>();

            collector.Setup(x => x.WithLabels(It.IsAny<string[]>()))
                .Returns(child.Object);
            collector.SetupGet(x => x.Unlabelled).Returns(child.Object);
            return collector.Object;
        }
    }
}