using System;
using Microsoft.AspNetCore.Http;
using Moq;
using Prometheus;
using Shouldly;
using Xunit;

namespace O9d.Metrics.AspNet.Tests
{
    public class AspNetMetricsObserverTests
    {
        private readonly DefaultHttpContext _httpContext;
        private readonly TestMetrics _metrics;

        public AspNetMetricsObserverTests()
        {
            _httpContext = new DefaultHttpContext();
            _metrics = new TestMetrics();
        }

        [Fact]
        public void Can_track_requests()
        {
            _httpContext.SetOperation("op");

            var observer = new AspNetMetricsObserver(new AspNetMetricsOptions(), _metrics);

            observer.OnNext(new("Microsoft.AspNetCore.Routing.HttpRequestIn.Start", _httpContext));

            observer.OnNext(new("Microsoft.AspNetCore.Routing.EndpointMatched", _httpContext));

            var metric = _metrics.GetMetric<IGauge>("http_server_requests_in_progress");
            metric.ShouldNotBeNull();
            metric.Collector.Verify(x => x.WithLabels(It.IsIn("op")), Times.Once);
            metric.Child.Verify(x => x.Inc(1), Times.Once);
        
            _httpContext.Response.StatusCode = 200;
            observer.OnNext(new("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", _httpContext));
        }
    }
}