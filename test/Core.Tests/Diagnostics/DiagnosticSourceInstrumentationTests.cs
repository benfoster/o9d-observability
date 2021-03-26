using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using O9d.Observability.Diagnostics;
using O9d.Observability.Tests.Diagnostics;
using Shouldly;
using Xunit;

namespace O9d.Observability.Core.Tests.Diagnostics
{
    public class DiagnosticSourceInstrumentationTests
    {
        private static readonly DiagnosticSource TestSource = new DiagnosticListener("TestSource");

        private readonly List<KeyValuePair<string, object?>> _events;
        private readonly IObserver<KeyValuePair<string, object?>> _observer;

        public DiagnosticSourceInstrumentationTests()
        {
            _events = new List<KeyValuePair<string, object?>>();
            _observer = new DelegateObserver(e => _events.Add(e));
        }

        [Fact]
        public async Task Can_subscribe_to_diagnostic_source()
        {
            using var instrumentation =
                new DiagnosticSourceInstrumentation(_ => _observer, l => l.Name == "TestSource", null);

            await instrumentation.StartAsync(CancellationToken.None);

            TestSource.Write("TestEvent", new TestEvent());
            _events.Count.ShouldBe(1);
            _events.Find(kvp => kvp.Key == "TestEvent").Value.ShouldNotBeNull();
        }

        [Fact]
        public async Task Can_subscribe_with_filter()
        {
            using var instrumentation =
                new DiagnosticSourceInstrumentation(
                    _ => _observer,
                    l => l.Name == "TestSource",
                    (eventType, _, _) => eventType == "SpecialEvent");

            await instrumentation.StartAsync(CancellationToken.None);

            if (TestSource.IsEnabled("TestEvent"))
            {
                TestSource.Write("TestEvent", new TestEvent());
            }

            if (TestSource.IsEnabled("SpecialEvent"))
            {
                TestSource.Write("SpecialEvent", new TestEvent());
            }

            _events.Count.ShouldBe(1);
        }

        class TestEvent
        {

        }
    }
}