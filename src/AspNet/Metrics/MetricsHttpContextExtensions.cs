using System;
using Microsoft.AspNetCore.Http;

namespace O9d.Observability.AspNet.Metrics
{
    public static class MetricsHttpContextExtensions
    {
        private const string Prefix = "O9d.Observability";
        private const string RequestTimeStampKey = Prefix + "_RequestTimeStamp";
        private const string RequestOperationKey = Prefix + "_RequestOperation";
        
        public static void SetOperation(this HttpContext httpContext, string operation)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            httpContext.Items[RequestOperationKey] = operation;
        }

        public static string? GetOperation(this HttpContext httpContext)
        {
            if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));
            httpContext.Items.TryGetValue(RequestOperationKey, out object? operation);
            return operation as string;
        }
    }
}