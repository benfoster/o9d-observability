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
            observer.OnHttpRequestStarted(_httpContext);
            _httpContext.GetRequestDuration().ShouldNotBe(TimeSpan.Zero);
        }

        [Fact]
        public void Sli_errors_increment_counter()
        {
            _httpContext.SetSliError(ErrorType.InternalDependency, "client-service");
            _httpContext.SetOperation("error-op");

            var observer = new AspNetMetricsObserver(new AspNetMetricsOptions(), _metrics);
            observer.OnHttpRequestStarted(_httpContext);
            observer.OnHttpRequestCompleted(_httpContext);

            var metric = _metrics.GetMetric<ICounter>("http_server_errors_total");
            metric.ShouldNotBeNull();
            metric.Collector.Verify(x => x.WithLabels("error-op", "internal_dependency", "client-service"), Times.Once);
            metric.Child.Verify(x => x.Inc(1), Times.Once);
        }

        [Fact]
        public void Sli_exceptions_set_sli_error()
        {
            var observer = new AspNetMetricsObserver(new AspNetMetricsOptions(), _metrics);
            observer.OnUnhandledException(_httpContext, new SliException(ErrorType.InternalDependency, "dep"));

            _httpContext.HasError(out var error).ShouldBeTrue();
            error.ShouldNotBeNull();
            error.Value.Item1.ShouldBe(ErrorType.InternalDependency);
            error.Value.Item2.ShouldBe("dep");
        }

        [Fact]
        public void Client_errors_increment_error_counter()
        {
            _httpContext.SetOperation("op");

            var observer = new AspNetMetricsObserver(new AspNetMetricsOptions(), _metrics);
            observer.OnHttpRequestStarted(_httpContext);
            observer.OnEndpointMatched(_httpContext);
            _httpContext.Response.StatusCode = 422;
            observer.OnHttpRequestCompleted(_httpContext);

            var metric = _metrics.GetMetric<ICounter>("http_server_errors_total");
            metric.ShouldNotBeNull();
            metric.Collector.Verify(x => x.WithLabels("op", "invalid_request", String.Empty), Times.Once);
            metric.Child.Verify(x => x.Inc(1), Times.Once);
        }

        [Fact]
        public void Server_errors_increment_error_counter()
        {
            _httpContext.SetOperation("op");

            var observer = new AspNetMetricsObserver(new AspNetMetricsOptions(), _metrics);
            observer.OnHttpRequestStarted(_httpContext);
            observer.OnEndpointMatched(_httpContext);
            _httpContext.Response.StatusCode = 504;
            observer.OnHttpRequestCompleted(_httpContext);

            var metric = _metrics.GetMetric<ICounter>("http_server_errors_total");
            metric.ShouldNotBeNull();
            metric.Collector.Verify(x => x.WithLabels("op", "internal", String.Empty), Times.Once);
            metric.Child.Verify(x => x.Inc(1), Times.Once);
        }

        [Fact]
        public void Can_track_requests()
        {
            _httpContext.SetOperation("op");

            var observer = new AspNetMetricsObserver(new AspNetMetricsOptions(), _metrics);

            observer.OnHttpRequestStarted(_httpContext);

            observer.OnEndpointMatched(_httpContext);

            var metric = _metrics.GetMetric<IGauge>("http_server_requests_in_progress");
            metric.ShouldNotBeNull();
            metric.Collector.Verify(x => x.WithLabels("op"), Times.Once);
            metric.Child.Verify(x => x.Inc(1), Times.Once);

            _httpContext.Response.StatusCode = 200;
            observer.OnHttpRequestCompleted(_httpContext);
        }

        [Fact]
        public void With_prometheus_metrics()
        {
            _httpContext.SetOperation("op");

            var observer = new AspNetMetricsObserver(new AspNetMetricsOptions(), new PrometheusMetrics());
            observer.OnHttpRequestStarted(_httpContext);
            observer.OnEndpointMatched(_httpContext);
            _httpContext.Response.StatusCode = 200;
            observer.OnHttpRequestCompleted(_httpContext);
        }

        [Fact]
        public void Can_configure_error_total_counter()
        {
            bool invoked = false;
            var options = new AspNetMetricsOptions
            {
                ConfigureErrorTotalCounter = config => invoked = true
            };

            _ = new AspNetMetricsObserver(options, _metrics);
            invoked.ShouldBeTrue();
        }

        [Fact]
        public void Throws_if_missing_default_error_total_counter_labels()
        {
            var options = new AspNetMetricsOptions
            {
                ConfigureErrorTotalCounter = config => config.LabelNames = new[] { "foo" }
            };

            Assert.Throws<ArgumentException>(() => new AspNetMetricsObserver(options, _metrics));
        }

        [Fact]
        public void Can_configure_requests_in_progress_gauge()
        {
            bool invoked = false;
            var options = new AspNetMetricsOptions
            {
                ConfigureRequestsInProgressGauge = config => invoked = true
            };

            _ = new AspNetMetricsObserver(options, _metrics);
            invoked.ShouldBeTrue();
        }

        [Fact]
        public void Throws_if_missing_default_requests_in_progress_labels()
        {
            var options = new AspNetMetricsOptions
            {
                ConfigureRequestsInProgressGauge = config => config.LabelNames = new[] { "foo" }
            };

            Assert.Throws<ArgumentException>(() => new AspNetMetricsObserver(options, _metrics));
        }

        [Fact]
        public void Uses_a_histogram_for_request_duration_by_default()
        {
            _ = new AspNetMetricsObserver(new(), _metrics);
            _metrics.GetMetric<IHistogram>("http_server_request_duration_seconds").ShouldNotBeNull();
            _metrics.GetMetric<ISummary>("http_server_request_duration_seconds").ShouldBeNull();
        }

        [Fact]
        public void Can_use_summary_for_request_duration()
        {
            _ = new AspNetMetricsObserver(new() { RequestDurationMetricType = ObserverMetricType.Summary }, _metrics);
            _metrics.GetMetric<IHistogram>("http_server_request_duration_seconds").ShouldBeNull();
            _metrics.GetMetric<ISummary>("http_server_request_duration_seconds").ShouldNotBeNull();
        }

        [Fact]
        public void Can_configure_request_duration_histogram()
        {
            bool invoked = false;
            var options = new AspNetMetricsOptions
            {
                ConfigureRequestDurationHistogram = config => invoked = true
            };

            _ = new AspNetMetricsObserver(options, _metrics);
            invoked.ShouldBeTrue();
        }

        [Fact]
        public void Throws_if_missing_request_duration_histogram_labels()
        {
            var options = new AspNetMetricsOptions
            {
                ConfigureRequestDurationHistogram = config => config.LabelNames = new[] { "foo" }
            };

            Assert.Throws<ArgumentException>(() => new AspNetMetricsObserver(options, _metrics));
        }

        [Fact]
        public void Can_configure_request_duration_summary()
        {
            bool invoked = false;
            var options = new AspNetMetricsOptions
            {
                ConfigureRequestDurationSummary = config => invoked = true,
                RequestDurationMetricType = ObserverMetricType.Summary
            };

            _ = new AspNetMetricsObserver(options, _metrics);
            invoked.ShouldBeTrue();
        }

        [Fact]
        public void Throws_if_missing_request_duration_summary_labels()
        {
            var options = new AspNetMetricsOptions
            {
                ConfigureRequestDurationSummary = config => config.LabelNames = new[] { "foo" },
                RequestDurationMetricType = ObserverMetricType.Summary
            };

            Assert.Throws<ArgumentException>(() => new AspNetMetricsObserver(options, _metrics));
        }
    }
}