using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Prometheus;

namespace O9d.Observability.AspNet.Metrics
{
    internal class AspNetMetricsObserver : IObserver<KeyValuePair<string, object?>>
    {
        private static readonly Gauge HttpRequestsInProgress = Prometheus.Metrics.CreateGauge("http_server_requests_in_progress", "The number of HTTP requests currently being processed by the application", new GaugeConfiguration
        {
            SuppressInitialValue = true,
            LabelNames = new[] { "operation" }
        });

        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
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
    }
}