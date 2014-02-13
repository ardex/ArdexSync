using System;
using System.Collections.Generic;
using System.Reflection;

using Ardex.EntityConversions.Implementation;

namespace Ardex.EntityConversions
{
    /// <summary>
    /// Provides fluent API for reflection-based conversion between data types.
    /// </summary>
    public static class EntityConversion
    {
        /// <summary>
        /// Starts the conversion from the given type.
        /// </summary>
        public static ISingleConversionSource<TSource> Convert<TSource>(TSource source)
        {
            if (source == null) throw new ArgumentNullException("source");

            return new SingleConversionSource<TSource>(source);
        }

        /// <summary>
        /// Starts the conversion from the given type.
        /// </summary>
        public static IEnumerableConversionSource<TSource> ConvertAll<TSource>(IEnumerable<TSource> source)
        {
            if (source == null) throw new ArgumentNullException("source");

            return new EnumerableConversionSource<TSource>(source);
        }

        /// <summary>
        /// Validates the types to ensure the conversion will succeed.
        /// </summary>
        internal static void ValidateTypes(Type interfaceType, Type resultType)
        {
            if (!interfaceType.GetTypeInfo().IsInterface)
                throw new ArgumentException("Source must be an interface type.");

            if (resultType.GetTypeInfo().IsInterface)
                throw new ArgumentException("Target type must be a concrete type, not an interface.");

            if (!interfaceType.GetTypeInfo().IsAssignableFrom(resultType.GetTypeInfo()))
                throw new ArgumentException("Target type is not compatible with base (interface) type.");

            if (interfaceType == resultType)
                throw new ArgumentException("Entity and interface types must be different.");
        }
    }
}