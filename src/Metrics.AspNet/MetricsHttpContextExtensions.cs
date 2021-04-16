using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using O9d.Observability;

namespace O9d.Metrics.AspNet
{
    public static class MetricsHttpContextExtensions
    {
        private const string Prefix = "O9d.Observability";

        private const string RequestOperationKey = Prefix + "_ReqOp";
        internal static void SetOperation(this HttpContext httpContext, string operation)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            httpContext.Items[RequestOperationKey] = operation;
        }

        internal static string? GetOperation(this HttpContext httpContext)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            httpContext.Items.TryGetValue(RequestOperationKey, out object? operation);
            return operation as string;
        }

        private const string RequestTimeStampKey = Prefix + "_ReqTs";
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
        internal static void SetRequestTimestamp(this HttpContext httpContext, long? timestamp = null)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            httpContext.Items[RequestTimeStampKey] = timestamp ?? Stopwatch.GetTimestamp();
        }

        public static TimeSpan GetRequestDuration(this HttpContext httpContext, long? currentTimestamp = null)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

            if (httpContext.Items.TryGetValue(RequestTimeStampKey, out object? value) && value is long requestStart)
            {
                long elapsed = (long)(TimestampToTicks * ((currentTimestamp ?? Stopwatch.GetTimestamp()) - requestStart));
                return new TimeSpan(elapsed);
            }

            return TimeSpan.Zero;
        }

        private const string SliErrorTypeKey = Prefix + "_ErrType";
        private const string SliErrorDependencyKey = Prefix + "_ErrDependency";

        internal static bool HasError(this HttpContext httpContext, out (ErrorType, string?)? error)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            error = default;

            if (httpContext.Items.TryGetValue(SliErrorTypeKey, out object? value) && value is ErrorType errorType)
            {
                httpContext.Items.TryGetValue(SliErrorDependencyKey, out object? dependency);
                error = (errorType, dependency as string);
                return true;
            }

            if (httpContext.GetCurrentException() is SliException sliEx)
            {
                error = (sliEx.ErrorType, sliEx.Dependency);
                return true;
            }

            return false;
        }

        public static void SetSliError(this HttpContext httpContext, ErrorType errorType, string? errorDependency = null)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            httpContext.Items[SliErrorTypeKey] = errorType;

            if (!string.IsNullOrWhiteSpace(errorDependency))
            {
                httpContext.Items[SliErrorDependencyKey] = errorDependency;
            }
        }

        internal static Exception? GetCurrentException(this HttpContext httpContext)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            ExceptionHandlerFeature? exceptionFeature = httpContext.Features?.Get<ExceptionHandlerFeature>();

            return exceptionFeature?.Error;
        }
    }
}