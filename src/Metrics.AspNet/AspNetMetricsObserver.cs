using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Prometheus;
using O9d.Observability;

namespace O9d.Metrics.AspNet
{
    internal class AspNetMetricsObserver : IObserver<KeyValuePair<string, object?>>
    {
        private static readonly Gauge HttpRequestsInProgress = Prometheus.Metrics
            .CreateGauge("http_server_requests_in_progress", "The number of HTTP requests currently being processed by the application", 
            new GaugeConfiguration
            {
                SuppressInitialValue = true,
                LabelNames = new[] { "operation" }
            });

        private static readonly Counter HttpErrorsTotal = Prometheus.Metrics
            .CreateCounter("http_server_errors_total", "The number of HTTP requests resulting in an error",
            new CounterConfiguration
            {
                SuppressInitialValue = true,
                LabelNames = new[] { "operation", "sli_error_type", "sli_dependency_name" }
            });

        private static readonly ICollector<IObserver> HttpRequestDuration = Prometheus.Metrics
            .CreateSummary("http_server_request_duration_seconds", "The duration in seconds that HTTP requests take to process",
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

        private readonly AspNetMetricsOptions _options;

        public AspNetMetricsObserver(AspNetMetricsOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void OnNext(KeyValuePair<string, object?> kvp)
        {
            switch (kvp.Key)
            {
                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                    OnHttpRequestStarted(kvp.Value as HttpContext);
                    break;
                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                    OnHttpRequestCompleted(kvp.Value as HttpContext);
                    break;
                case "Microsoft.AspNetCore.Routing.EndpointMatched":
                    OnEndpointMatched(kvp.Value as HttpContext);
                    break;
            }
        }

        protected virtual void OnHttpRequestStarted(HttpContext? httpContext)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

            if (_options.ShouldInstrument(httpContext.Request.Path))
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

            HttpRequestsInProgress
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

            HttpRequestDuration
                .WithLabels(operation, httpContext.Response.StatusCode.ToString())
                .Observe(httpContext.GetRequestDuration().TotalSeconds);

            if (HasError(httpContext, out (ErrorType Type, string? Dependency)? error))
            {
                HttpErrorsTotal
                    .WithLabels(operation, error!.Value.Type.ToString(), error!.Value.Dependency ?? string.Empty)
                    .Inc();
            }

            HttpRequestsInProgress
                .WithLabels(operation)
                .Dec();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        private static string? GetOperation(HttpContext httpContext)
        {
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