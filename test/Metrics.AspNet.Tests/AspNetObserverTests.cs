using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace O9d.Metrics.AspNet.Tests
{
    public class AspNetObserverTests
    {
        private readonly TestObserver _observer;
        private readonly DefaultHttpContext _httpContext;

        public AspNetObserverTests()
        {
            _observer = new TestObserver();
            _httpContext = new DefaultHttpContext();
        }

        [Fact]
        public void Handles_unhandled_exception_events()
        {
            var ex = new Exception();

            var @event = new
            {
                httpContext = _httpContext,
                exception = ex
            };

            _observer.HandleEvent("Microsoft.AspNetCore.Diagnostics.UnhandledException", @event);

            _observer.OnUnhandledExceptionInvoked.ShouldBeTrue();
            _observer.UnhandledException.ShouldBe(ex);
            _observer.HttpContext.ShouldBe(_httpContext);
        }

        [Fact]
        public void Skips_invalid_exception_events()
        {
            // Invalid event schema
            var @event = new
            {
                context = _httpContext,
                ex = new Exception()
            };

            _observer.HandleEvent("Microsoft.AspNetCore.Diagnostics.UnhandledException", @event);

            _observer.OnUnhandledExceptionInvoked.ShouldBeFalse();
        }

        [Fact]
        public void Skips_valid_exception_events_with_null_fields()
        {
            // Invalid event schema
            var @event = new
            {
                context = default(HttpContext),
                ex = default(Exception)
            };

            _observer.HandleEvent("Microsoft.AspNetCore.Diagnostics.UnhandledException", @event);

            _observer.OnUnhandledExceptionInvoked.ShouldBeFalse();
        }

        [Fact]
        public void Handles_request_start_events()
        {
            _observer.HandleEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Start", _httpContext);
            _observer.HttpContext.ShouldNotBeNull();   
        }

        [Fact]
        public void Handles_endpoint_matched_events()
        {
            _observer.HandleEvent("Microsoft.AspNetCore.Routing.EndpointMatched", _httpContext);
            _observer.HttpContext.ShouldNotBeNull();   
        }

        [Fact]
        public void Handles_request_stop_events()
        {
            _observer.HandleEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", _httpContext);
            _observer.HttpContext.ShouldNotBeNull();   
        }

        private class TestObserver : AspNetObserver
        {
            public HttpContext? HttpContext { get; private set; }
            public Exception? UnhandledException { get; private set; }
            public bool OnUnhandledExceptionInvoked { get; private set; }

            public override void OnUnhandledException(HttpContext httpContext, Exception exception)
            {
                HttpContext = httpContext;
                UnhandledException = exception;
                OnUnhandledExceptionInvoked = true;
            }

            public override void OnHttpRequestStarted(HttpContext httpContext)
            {
                HttpContext = httpContext;
            }

            public override void OnEndpointMatched(HttpContext httpContext)
            {
                HttpContext = httpContext;
            }

            public override void OnHttpRequestCompleted(HttpContext httpContext)
            {
                HttpContext = httpContext;
            }

            public void HandleEvent(string key, object @event)
            {
                ((IObserver<KeyValuePair<string, object?>>)this)
                    .OnNext(new(key, @event));
            }
        }
    }
}