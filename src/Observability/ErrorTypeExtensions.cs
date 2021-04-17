namespace O9d.Observability
{
    public static class ErrorTypeExtensions
    {
        /// <summary>
        /// Returns the string value representation of the error type for outputting to external tools
        /// </summary>
        /// <param name="errorType">The type of error</param>
        /// <returns></returns>
        public static string GetStringValue(this ErrorType errorType)
        {
            return errorType switch
            {
                ErrorType.InvalidRequest => "invalid_request",
                ErrorType.Internal => "internal",
                ErrorType.ExternalDependency => "external_dependency",
                ErrorType.InternalDependency => "internal_dependency",
                _ => string.Empty
            };
        }
    }
}