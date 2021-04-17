using System;
using Microsoft.AspNetCore.Http;
using Moq;
using O9d.Observability;
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
        public void Request_timestamp_is_set_on_request_start()
        {
            var observer = new AspNetMetricsObserver(new AspNetMetricsOptions(), _metrics);
            observer.OnNext(new("Microsoft.AspNetCore.Hosting.HttpRequestIn.Start", _httpContext));

            _httpContext.GetRequestDuration().ShouldNotBe(TimeSpan.Zero);
        }

        [Fact]
        public void Sli_errors_increment_counter()
        {
            _httpContext.SetSliError(ErrorType.InternalDependency, "client-service");
            _httpContext.SetOperation("error-op");

            var observer = new AspNetMetricsObserver(new AspNetMetricsOptions(), _metrics);
            observer.OnNext(new("Microsoft.AspNetCore.Routing.HttpRequestIn.Start", _httpContext));
            observer.OnNext(new("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", _httpContext));

            var metric = _metrics.GetMetric<ICounter>("http_server_errors_total");
            metric.ShouldNotBeNull();
            metric.Collector.Verify(x => x.WithLabels("error-op", "internal_dependency", "client-service"), Times.Once);
            metric.Child.Verify(x => x.Inc(1), Times.Once);
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
            metric.Collector.Verify(x => x.WithLabels("op"), Times.Once);
            metric.Child.Verify(x => x.Inc(1), Times.Once);
        
            _httpContext.Response.StatusCode = 200;
            observer.OnNext(new("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", _httpContext));
        }

        [Fact]
        public void With_prometheus_metrics()
        {
            _httpContext.SetOperation("op");

            var observer = new AspNetMetricsObserver(new AspNetMetricsOptions(), new PrometheusMetrics());
            observer.OnNext(new("Microsoft.AspNetCore.Routing.HttpRequestIn.Start", _httpContext));
            observer.OnNext(new("Microsoft.AspNetCore.Routing.EndpointMatched", _httpContext));
            _httpContext.Response.StatusCode = 200;
            observer.OnNext(new("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", _httpContext));
        }
    }
}