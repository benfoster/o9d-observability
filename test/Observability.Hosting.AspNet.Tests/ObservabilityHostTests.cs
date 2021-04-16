using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using O9d.Observability.Hosting.AspNet;
using Xunit;

namespace O9d.Observability.Hosting.AspNet.Tests
{
    public class ObservabilityHostTests
    {
        [Fact]
        public void Throws_if_instrumentations_are_null()
        {
            Assert.Throws<ArgumentNullException>(() => new ObservabilityHost(null!));
        }

        [Fact]
        public async Task It_starts_each_instrumentation()
        {
            var inst1 = new Mock<IInstrumentation>();
            var inst2 = new Mock<IInstrumentation>();

            var host = new ObservabilityHost(new[] { inst1.Object, inst2.Object });
            
            using var cts = new CancellationTokenSource();
            await host.StartAsync(cts.Token);

            inst1.Verify(x => x.StartAsync(cts.Token));
            inst2.Verify(x => x.StartAsync(cts.Token));
        }

        [Fact]
        public async Task It_disposes_instrumentations_on_stop()
        {
            var inst1 = new Mock<IDisposableInstrumentation>();

            var host = new ObservabilityHost(new[] { inst1.Object });
            
            using var cts = new CancellationTokenSource();
            await host.StopAsync(cts.Token);

            inst1.Verify(x => x.Dispose());
        }

        public interface IDisposableInstrumentation : IInstrumentation, IDisposable
        {

        }
    }
}