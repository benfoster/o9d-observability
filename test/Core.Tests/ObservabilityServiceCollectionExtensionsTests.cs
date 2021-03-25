using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Xunit;

namespace O9d.Observability.Core.Tests
{
    public class ObservabilityServiceCollectionExtensionsTests
    {        
        [Fact]
        public void Registers_the_observability_host()
        {
            var serviceProvider = new ServiceCollection()
                .AddObservability(builder => {})
                .BuildServiceProvider();

            serviceProvider.GetService<IObservabilityHost>().ShouldNotBeNull();
        }
    }
}