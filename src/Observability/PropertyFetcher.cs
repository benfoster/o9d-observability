using System;
using System.Linq;
using System.Reflection;

namespace O9d.Observability
{
    /// <summary>
    /// PropertyFetcher fetches a property from an object.
    /// Copied with love from https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/DiagnosticSourceInstrumentation/PropertyFetcher.cs
    /// </summary>
    /// <typeparam name="T">The type of the property being fetched.</typeparam>
    public class PropertyFetcher<T>
    {
        private readonly string _propertyName;
        private PropertyFetch? _innerFetcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyFetcher{T}"/> class.
        /// </summary>
        /// <param name="propertyName">Property name to fetch.</param>
        public PropertyFetcher(string propertyName)
        {            
            _propertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        }

        /// <summary>
        /// Fetch the property from the object.
        /// </summary>
        /// <param name="obj">Object to be fetched.</param>
        /// <returns>Property fetched.</returns>
        public T? Fetch(object obj)
        {
            if (!TryFetch(obj, out T? value))
            {
                throw new ArgumentException("Supplied object was null or did not match the expected type.", nameof(obj));
            }

            return value;
        }

        /// <summary>
        /// Try to fetch the property from the object.
        /// </summary>
        /// <param name="obj">Object to be fetched.</param>
        /// <param name="value">Fetched value.</param>
        /// <returns><see langword="true"/> if the property was fetched.</returns>
        public bool TryFetch(object? obj, out T? value)
        {
            if (obj is null)
            {
                value = default;
                return false;
            }

            if (_innerFetcher is null)
            {
                var type = obj.GetType().GetTypeInfo();
                var property = type.DeclaredProperties.FirstOrDefault(p => string.Equals(p.Name, _propertyName, StringComparison.InvariantCultureIgnoreCase));
                if (property is null)
                {
                    property = type.GetProperty(_propertyName);
                }

                _innerFetcher = PropertyFetch.FetcherForProperty(property);
            }

            return _innerFetcher.TryFetch(obj, out value);
        }

        // see https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSourceEventSource.cs
        private class PropertyFetch
        {
            /// <summary>
            /// Create a property fetcher from a .NET Reflection PropertyInfo class that
            /// represents a property of a particular type.
            /// </summary>
            public static PropertyFetch FetcherForProperty(PropertyInfo? propertyInfo)
            {
                if (propertyInfo is null || !typeof(T).IsAssignableFrom(propertyInfo.PropertyType))
                {
                    // returns null on any fetch.
                    return new PropertyFetch();
                }

                var typedPropertyFetcher = typeof(TypedPropertyFetch<,>);

                var instantiatedTypedPropertyFetcher = typedPropertyFetcher.MakeGenericType(
                    typeof(T), propertyInfo.DeclaringType!, propertyInfo.PropertyType);
                
                return (PropertyFetch)Activator.CreateInstance(instantiatedTypedPropertyFetcher, propertyInfo)!;
            }

            public virtual bool TryFetch(object obj, out T? value)
            {
                value = default;
                return false;
            }

            private class TypedPropertyFetch<TDeclaredObject, TDeclaredProperty> : PropertyFetch
                where TDeclaredProperty : T
            {
                private readonly Func<TDeclaredObject, TDeclaredProperty> _propertyFetch;

                public TypedPropertyFetch(PropertyInfo property)
                {
                    if (property is null) throw new ArgumentNullException(nameof(property));
                    _propertyFetch = (Func<TDeclaredObject, TDeclaredProperty>)property.GetMethod!.CreateDelegate(typeof(Func<TDeclaredObject, TDeclaredProperty>));
                }

                public override bool TryFetch(object obj, out T? value)
                {
                    if (obj is TDeclaredObject o)
                    {
                        value = _propertyFetch(o);
                        return true;
                    }

                    value = default;
                    return false;
                }
            }
        }
    }
}