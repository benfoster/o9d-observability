using System;

namespace O9d.Observability
{
    public class SliException : Exception
    {
        public SliException(ErrorType errorType, string dependency)
        {
            Dependency = dependency;
            ErrorType = errorType;
        }
        
        public ErrorType ErrorType { get; }
        public string Dependency { get; }
    }
}