using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using O9d.Observability;

namespace O9d.Metrics.AspNet
{
    public abstract class AspNetObserver : IObserver<KeyValuePair<string, object?>>
    {        
        private readonly PropertyFetcher<Exception> _exceptionFetcher = new("exception");
        private readonly PropertyFetcher<HttpContext> _exceptionContextFetcher = new("httpContext");
        
        public virtual void OnHttpRequestStarted(HttpContext httpContext)
        {
        }

        public virtual void OnEndpointMatched(HttpContext httpContext)
        {
        }

        public virtual void OnUnhandledException(HttpContext httpContext, Exception exception)
        {
        }

        public virtual void OnHttpRequestCompleted(HttpContext httpContext)
        {
        }

        void IObserver<KeyValuePair<string, object?>>.OnNext(KeyValuePair<string, object?> kvp)
        {
            if (kvp.Value is null)
                return; // log?
            
            switch (kvp.Key)
            {
                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                    OnHttpRequestStarted((HttpContext)kvp.Value);
                    break;
                case "Microsoft.AspNetCore.Routing.EndpointMatched":
                    OnEndpointMatched((HttpContext)kvp.Value);
                    break;
                // Ref https://github.com/dotnet/aspnetcore/blob/52eff90fbcfca39b7eb58baad597df6a99a542b0/src/Middleware/Diagnostics/test/UnitTests/TestDiagnosticListener.cs
                case "Microsoft.AspNetCore.Diagnostics.UnhandledException":
                    ProcessException(kvp);
                    break;
                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                    OnHttpRequestCompleted((HttpContext)kvp.Value);
                    break;
            }
        }

        void IObserver<KeyValuePair<string, object?>>.OnCompleted()
        {
            // Not used
        }

        void IObserver<KeyValuePair<string, object?>>.OnError(Exception error)
        {
            // Not used
        }

        private void ProcessException(KeyValuePair<string, object?> kvp)
        {
            if (kvp.Value is not null 
                && _exceptionFetcher.TryFetch(kvp.Value, out Exception? ex)
                && ex is {}
                && _exceptionContextFetcher.TryFetch(kvp.Value, out HttpContext? httpContext)
                && httpContext is {}
            )
            {
                OnUnhandledException(httpContext, ex);
            }
        }        
    }
}