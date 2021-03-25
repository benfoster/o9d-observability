using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using O9d.Observability.Internal;

namespace O9d.Observability.Core.Tests.Internal
{
    public class ObservabilityBuilderTests
    {
        [Fact]
        public void Can_register_instrumentation()
        {
            var provider = new ServiceCollection()
                .AddObservability(builder =>
                {
                    builder.AddInstrumentation(_ => new WebInstrumentation());
                })
                .BuildServiceProvider();

            provider.GetService<IInstrumentation>().ShouldNotBeNull();
        }

        [Fact]
        public void Can_register_multiple_instrumentations()
        {
            var provider = new ServiceCollection()
                .AddObservability(builder =>
                {
                    builder.AddInstrumentation(_ => new WebInstrumentation());
                    builder.AddInstrumentation(_ => new DatabaseInstrumentation());
                })
                .BuildServiceProvider();

            var instrumentations = provider.GetServices<IInstrumentation>();
            instrumentations.Count().ShouldBe(2);
            instrumentations.ShouldContain(i => i is WebInstrumentation);
            instrumentations.ShouldContain(i => i is DatabaseInstrumentation);
        }

        [Fact]
        public void Can_register_instrumentation_with_dependencies()
        {
            var provider = new ServiceCollection()
                .AddTransient<Dependency>()
                .AddObservability(builder =>
                {
                    builder.AddInstrumentation(s => new DependencyInstrumentation(s.GetRequiredService<Dependency>()));
                })
                .BuildServiceProvider();

            provider.GetService<IInstrumentation>().ShouldNotBeNull();
        }

        [Fact]
        public void Throws_if_instrumentation_factory_is_null()
        {
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() => new ObservabilityBuilder(services).AddInstrumentation(null!));
        }

        class WebInstrumentation : IInstrumentation
        {
            public Task StartAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        class DatabaseInstrumentation : IInstrumentation
        {            
            public Task StartAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        class DependencyInstrumentation : IInstrumentation
        {
            public DependencyInstrumentation(Dependency _)
            {

            }
            
            public Task StartAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        class Dependency {}
    }
}