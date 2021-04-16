using Microsoft.AspNetCore.Http;
using Xunit;

namespace O9d.Metrics.AspNet.Tests
{
    public class AspNetMetricsObserverTests
    {
        private readonly DefaultHttpContext _httpContext;

        public AspNetMetricsObserverTests()
        {
            _httpContext = new DefaultHttpContext();
        }

        [Fact]
        public void Can_track_requests()
        {
            _httpContext.SetOperation("op");

            var observer = new AspNetMetricsObserver(new AspNetMetricsOptions());

            observer.OnNext(new("Microsoft.AspNetCore.Routing.HttpRequestIn.Start", _httpContext));
            observer.OnNext(new("Microsoft.AspNetCore.Routing.EndpointMatched", _httpContext));
            
            _httpContext.Response.StatusCode = 200;
            observer.OnNext(new("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", _httpContext));
        }
    }
}