using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using O9d.Observability;
using Prometheus;

namespace O9d.Metrics.AspNet
{
    internal class AspNetMetricsObserver : AspNetObserver
    {
        private readonly AspNetMetricsOptions _options;
        private readonly IMetrics _metrics;
        private readonly ICollector<ICounter> _httpErrorsTotalMetric;
        private readonly ICollector<IGauge> _httpRequestsInProgressMetric;
        private readonly ICollector<IObserver> _httpRequestDurationMetric;

        public AspNetMetricsObserver(AspNetMetricsOptions options)
            : this(options, new PrometheusMetrics())
        {
        }

        public AspNetMetricsObserver(AspNetMetricsOptions options, IMetrics metrics)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

            // Support overrides here
            _httpErrorsTotalMetric = CreateErrorTotalCounter();
            _httpRequestsInProgressMetric = CreateRequestsInProgressGauge();

            _httpRequestDurationMetric = options.RequestDurationMetricType switch
            {
                ObserverMetricType.Summary => CreateHttpRequestDurationSummary(),
                _ => CreateHttpRequestDurationHistogram()
            };
        }

        public override void OnHttpRequestStarted(HttpContext httpContext)
        {
            if (!_options.ShouldInstrument(httpContext.Request.Path))
            {
                return;
            }

            httpContext.SetRequestTimestamp();
        }

        public override void OnEndpointMatched(HttpContext httpContext)
        {
            if (!_options.ShouldInstrument(httpContext.Request.Path))
            {
                return;
            }

            string? operation = GetOperation(httpContext);

            if (operation is null)
            {
                return;
            }

            httpContext.SetOperation(operation); // Avoids calculating again later

            _httpRequestsInProgressMetric
                .WithLabels(operation)
                .Inc();
        }

        public override void OnUnhandledException(HttpContext httpContext, Exception exception)
        {
            if (exception is SliException sliException)
            {
                httpContext.SetSliError(sliException.ErrorType, sliException.Dependency);
            }
        }

        public override void OnHttpRequestCompleted(HttpContext httpContext)
        {
            if (!_options.ShouldInstrument(httpContext.Request.Path))
            {
                return;
            }

            string? operation = httpContext.GetOperation();

            if (operation is null)
            {
                return; // We don't care if this isn't a genuine operation
                // Should we be tracking missed operations?
            }

            _httpRequestDurationMetric
                .WithLabels(operation, httpContext.Response.StatusCode.ToString())
                .Observe(httpContext.GetRequestDuration().TotalSeconds);

            if (HasError(httpContext, out (ErrorType Type, string? Dependency)? error))
            {
                _httpErrorsTotalMetric
                    .WithLabels(operation, error!.Value.Type.GetStringValue(), error!.Value.Dependency ?? string.Empty)
                    .Inc();
            }

            _httpRequestsInProgressMetric
                .WithLabels(operation)
                .Dec();
        }

        protected virtual ICollector<ICounter> CreateErrorTotalCounter()
        {            
            var configuration = new CounterConfiguration
            {
                SuppressInitialValue = true,
                LabelNames = new[] { "operation", "sli_error_type", "sli_dependency" }
            };

            _options.ConfigureErrorTotalCounter?.Invoke(configuration);
            return _metrics.CreateCounter("http_server_errors_total", "The number of HTTP requests resulting in an error", configuration);
        }

        protected virtual ICollector<IGauge> CreateRequestsInProgressGauge()
        {
            var configuration = new GaugeConfiguration
            {
                SuppressInitialValue = true,
                LabelNames = new[] { "operation" }
            };

            _options.ConfigureRequestsInProgressGauge?.Invoke(configuration);
            return _metrics.CreateGauge("http_server_requests_in_progress", "The number of HTTP requests currently being processed by the application", configuration);
        }

        protected virtual ICollector<IObserver> CreateHttpRequestDurationSummary()
        {
            var configuration = new SummaryConfiguration
            {
                SuppressInitialValue = true,
                LabelNames = new[] { "operation", "status_code" },
                Objectives = new[]
                {
                    new QuantileEpsilonPair(0.5, 0.05),
                    new QuantileEpsilonPair(0.9, 0.05),
                    new QuantileEpsilonPair(0.95, 0.01),
                    new QuantileEpsilonPair(0.99, 0.005),
                }
            };

            _options.ConfigureRequestDurationSummary?.Invoke(configuration);
            return _metrics.CreateSummary("http_server_request_duration_seconds", "The duration in seconds that HTTP requests take to process", configuration);
        }

        protected virtual ICollector<IObserver> CreateHttpRequestDurationHistogram()
        {
            var configuration = new HistogramConfiguration
            {
                SuppressInitialValue = true,
                LabelNames = new[] { "operation", "status_code" },
                Buckets = new[] { 0.1, 0.2, 0.5, 1, 2 }
            };

            _options.ConfigureRequestDurationHistogram?.Invoke(configuration);
            return _metrics.CreateHistogram("http_server_request_duration_seconds", "The duration in seconds that HTTP requests take to process", configuration);
        }

        private static string? GetOperation(HttpContext httpContext)
        {
            string? operation = httpContext.GetOperation();

            if (operation != null)
            {
                return operation;
            }

            // TODO should we just move the below into the above extension?
            Endpoint? endpoint = httpContext.GetEndpoint();

            return endpoint?.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName
                ?? httpContext.Request.Method + " " + endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>()?
                .AttributeRouteInfo?.Template;
        }

        private static bool HasError(HttpContext httpContext, out (ErrorType, string?)? error)
        {
            error = default;

            if (httpContext.HasError(out var httpError))
            {
                error = httpError;
                return true;
            }

            switch (httpContext.Response.StatusCode)
            {
                case int s when s >= 400 && s < 500:
                    error = (ErrorType.InvalidRequest, default);
                    break;
                case int s when s >= 500:
                    error = (ErrorType.Internal, default);
                    break;
            }

            return error.HasValue;
        }
    }
}