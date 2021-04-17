using System;
using Microsoft.AspNetCore.Http;
using O9d.Observability;
using Shouldly;
using Xunit;

namespace O9d.Metrics.AspNet.Tests
{
    public class MetricsHttpContextExtensionsTests
    {
        private readonly DefaultHttpContext _httpContext;

        public MetricsHttpContextExtensionsTests()
        {
            _httpContext = new DefaultHttpContext();
        }
        
        [Fact]
        public void Can_set_and_get_operation()
        {
            _httpContext.SetOperation("op");
            _httpContext.GetOperation()?.ShouldBe("op");
        }

        [Fact]
        public void Can_set_and_get_timestamp()
        {
            _httpContext.SetRequestTimestamp();
            _httpContext.GetRequestDuration().ShouldBeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public void Can_set_and_check_sli_error()
        {
            _httpContext.SetSliError(ErrorType.Internal, "dependency");
            _httpContext.HasError(out var error).ShouldBeTrue();
            error.ShouldNotBeNull();
            error.Value.Item1.ShouldBe(ErrorType.Internal);
            error.Value.Item2.ShouldBe("dependency");
        }

        [Fact]
        public void Has_error_returns_false_if_no_error()
        {
            _httpContext.HasError(out _).ShouldBeFalse();
        }
    }
}