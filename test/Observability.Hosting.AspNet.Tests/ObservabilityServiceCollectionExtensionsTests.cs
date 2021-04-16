using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using O9d.Observability.Hosting.AspNet;
using Shouldly;
using Xunit;

namespace O9d.Observability.Hosting.AspNet.Tests
{
    public class ObservabilityServiceCollectionExtensionsTests
    {        
        [Fact]
        public void Registers_the_observability_host()
        {
            var serviceProvider = new ServiceCollection()
                .AddObservability(builder => {})
                .Services
                .BuildServiceProvider();

            serviceProvider.GetService<IHostedService>().ShouldBeOfType<ObservabilityHost>();
        }
    }
}