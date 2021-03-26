using System;
using System.Collections.Generic;
using Moq;
using O9d.Observability.Diagnostics;
using Xunit;

namespace O9d.Observability.Tests.Diagnostics
{
    public class DiagnosticObservabilityBuilderExtensionsTests
    {        
        [Fact]
        public void Registers_diagnostic_source_observer()
        {
            var builder = new Mock<IObservabilityBuilder>();

            builder.Object
                .AddDiagnosticSourceEventHandler("Source", Mock.Of<IObserver<KeyValuePair<string, object?>>>());

            builder.Verify(x =>
                x.AddInstrumentation(It.IsAny<Func<IServiceProvider, IInstrumentation>>()));
        }
    }
}