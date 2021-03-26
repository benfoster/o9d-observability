using System;
using System.Collections.Generic;

namespace O9d.Observability.Diagnostics
{
    public static class DiagnosticObservabilityBuilderExtensions
    {        
        public static IObservabilityBuilder AddDiagnosticSourceEventHandler(
            this IObservabilityBuilder builder, 
            string source, 
            IObserver<KeyValuePair<string, object?>> handler,
            Func<string, object?, object?, bool>? isEnabledFilter = null
        )
        {
            return builder.AddDiagnosticSourceEventHandler(source, _ => handler, isEnabledFilter);
        }
        
        public static IObservabilityBuilder AddDiagnosticSourceEventHandler(
            this IObservabilityBuilder builder, 
            string source, 
            Func<IServiceProvider, IObserver<KeyValuePair<string, object?>>> handlerFactory,
            Func<string, object?, object?, bool>? isEnabledFilter = null
        )
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("The diagnostic source name is required");
            if (handlerFactory is null) throw new ArgumentNullException(nameof(handlerFactory));
                        
            return builder.AddInstrumentation(sp =>
            {
                return new DiagnosticSourceInstrumentation(
                    _ => handlerFactory.Invoke(sp),
                    listener => listener.Name == source,
                    isEnabledFilter
                );
            });
        }
    }
}