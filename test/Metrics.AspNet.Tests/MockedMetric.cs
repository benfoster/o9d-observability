using Moq;
using Prometheus;

namespace O9d.Metrics.AspNet.Tests
{
    public class MockedMetric<TChild> where TChild : class, ICollectorChild
    {
        public MockedMetric(Mock<ICollector<TChild>> collector, Mock<TChild> child)
        {
            Collector = collector;
            Child = child;
        }

        public Mock<ICollector<TChild>> Collector { get; }
        public Mock<TChild> Child { get; }
    }
}