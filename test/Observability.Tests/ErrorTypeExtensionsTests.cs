using Shouldly;
using Xunit;

namespace O9d.Observability.Tests
{
    public class ErrorTypeExtensionsTests
    {
        [Theory]
        [InlineData(ErrorType.Internal, "internal")]
        [InlineData(ErrorType.InternalDependency, "internal_dependency")]
        [InlineData(ErrorType.ExternalDependency, "external_dependency")]
        [InlineData(ErrorType.InvalidRequest, "invalid_request")]
        public void Can_get_string_value(ErrorType errorType, string expected)
        {
            errorType.GetStringValue().ShouldBe(expected);
        }
    }
}