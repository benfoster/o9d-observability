using System;
using Microsoft.Extensions.DependencyInjection;

namespace O9d.Observability.Hosting.AspNet
{
    ///<inheritdoc/>
    internal class ObservabilityBuilder : IObservabilityBuilder
    {
        private readonly IServiceCollection _services;

        public ObservabilityBuilder(IServiceCollection services)
        {
            _services = services;
        }

        ///<inheritdoc/>
        public IServiceCollection Services => _services;
        
        ///<inheritdoc/>
        public IObservabilityBuilder AddInstrumentation(Func<IServiceProvider, IInstrumentation> instrumentationFactory)
        {
            _ = _services.AddTransient(instrumentationFactory ?? throw new ArgumentNullException(nameof(instrumentationFactory)));
            return this;
        }

        internal void Build()
        {
            _ = _services.AddHostedService<ObservabilityHost>();
        }
    }
}