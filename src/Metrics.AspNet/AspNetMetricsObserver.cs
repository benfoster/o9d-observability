using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Prometheus;
using O9d.Observability;
using Microsoft.AspNetCore.Mvc;

namespace O9d.Metrics.AspNet
{
    internal class AspNetMetricsObserver : IObserver<KeyValuePair<string, object?>>
    {
        private readonly AspNetMetricsOptions _options;
        private readonly IMetrics _metrics;
        private readonly ICollector<ICounter> _httpErrorsTotalMetric;
        private readonly ICollector<IGauge> _httpRequestsInProgressMetric;
        private readonly ICollector<IObserver> _httpRequestDurationMetric;
        private readonly PropertyFetcher<Exception> _exceptionFetcher = new("exception");
        private readonly PropertyFetcher<HttpContext> _exceptionContextFetcher = new("httpContext");

        public AspNetMetricsObserver(AspNetMetricsOptions options)
            : this(options, new PrometheusMetrics())
        {
        }

        public AspNetMetricsObserver(AspNetMetricsOptions options, IMetrics metrics)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

            // Support overrides here
            _httpErrorsTotalMetric = CreateHttpErrorsTotalMetric();
            _httpRequestsInProgressMetric = CreateHttpRequestsInProgressMetric();
            _httpRequestDurationMetric = CreateHttpRequestDurationMetric();
        }

        public void OnNext(KeyValuePair<string, object?> kvp)
        {
            switch (kvp.Key)
            {
                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                    OnHttpRequestStarted(kvp.Value as HttpContext);
                    break;
                case "Microsoft.AspNetCore.Routing.EndpointMatched":
                    OnEndpointMatched(kvp.Value as HttpContext);
                    break;
                // Ref https://github.com/dotnet/aspnetcore/blob/52eff90fbcfca39b7eb58baad597df6a99a542b0/src/Middleware/Diagnostics/test/UnitTests/TestDiagnosticListener.cs
                case "Microsoft.AspNetCore.Diagnostics.UnhandledException":
                    OnException(kvp);
                    break;
                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                    OnHttpRequestCompleted(kvp.Value as HttpContext);
                    break;
            }
        }

        private void OnException(KeyValuePair<string, object?> kvp)
        {
            if (kvp.Value is not null 
                && _exceptionFetcher.TryFetch(kvp.Value, out Exception? ex)
                && ex is SliException sliEx
                && _exceptionContextFetcher.TryFetch(kvp.Value, out HttpContext? httpContext)
            )
            {
                httpContext?.SetSliError(sliEx.ErrorType, sliEx.Dependency);
            }
        }

        protected virtual void OnHttpRequestStarted(HttpContext? httpContext)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

            if (!_options.ShouldInstrument(httpContext.Request.Path))
            {
                return;
            }

            httpContext.SetRequestTimestamp();
        }

        protected virtual void OnEndpointMatched(HttpContext? httpContext)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

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

        protected virtual void OnHttpRequestCompleted(HttpContext? httpContext)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

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

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        private ICollector<ICounter> CreateHttpErrorsTotalMetric()
            => _metrics.CreateCounter("http_server_errors_total", "The number of HTTP requests resulting in an error",
            new CounterConfiguration
            {
                SuppressInitialValue = true,
                LabelNames = new[] { "operation", "sli_error_type", "sli_dependency" }
            });

        private ICollector<IGauge> CreateHttpRequestsInProgressMetric()
            => _metrics.CreateGauge("http_server_requests_in_progress", "The number of HTTP requests currently being processed by the application", 
            new GaugeConfiguration
            {
                SuppressInitialValue = true,
                LabelNames = new[] { "operation" }
            });

        private ICollector<IObserver> CreateHttpRequestDurationMetric()
            => _metrics.CreateSummary("http_server_request_duration_seconds", "The duration in seconds that HTTP requests take to process",
             new SummaryConfiguration
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
             });

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