using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ardex.Util
{
    /// <summary>
    /// Provides fluent API for reflection-based conversion between data types.
    /// </summary>
    public static class EntityConversion
    {
        #region Public static methods

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

        #endregion

        #region Interfaces

        /// <summary>
        /// Wraps the source and exposes further conversion operations.
        /// </summary>
        public interface ISingleConversionSource<TSource>
        {
            /// <summary>
            /// Casts the source to the given type and returns a new conversion source.
            /// </summary>
            ISingleConversionSource<TInterface> As<TInterface>();

            /// <summary>
            /// Performs the conversion and returns the result.
            /// </summary>
            TResult To<TResult>() where TResult : TSource, new();
        }

        /// <summary>
        /// Wraps the source and exposes further conversion operations.
        /// </summary>
        public interface IEnumerableConversionSource<TSource>
        {
            /// <summary>
            /// Casts the source to the given type and returns a new conversion source.
            /// </summary>
            IEnumerableConversionSource<TInterface> As<TInterface>();

            /// <summary>
            /// Performs the conversion and yields the results.
            /// </summary>
            IEnumerable<TResult> To<TResult>() where TResult : TSource, new();
        }

        #endregion

        #region Private interface implementations

        /// <summary>
        /// Provides means of converting single objects.
        /// </summary>
        private sealed class SingleConversionSource<TSource> : ISingleConversionSource<TSource>
        {
            private readonly TSource __source;

            public SingleConversionSource(TSource source)
            {
                if (source == null) throw new ArgumentNullException("source");

                __source = source;
            }

            public ISingleConversionSource<TInterface> As<TInterface>()
            {
                var @base = (TInterface)(object)__source;

                return new SingleConversionSource<TInterface>(@base);
            }

            public TResult To<TResult>() where TResult : TSource, new()
            {
                EntityConversion.ValidateTypes(typeof(TSource), typeof(TResult));

                var newEntity = new TResult();
                var properties = typeof(TSource).GetProperties();

                foreach (var prop in properties)
                {
                    if (prop.CanRead && prop.CanWrite)
                    {
                        var value = prop.GetValue(__source, null);

                        prop.SetValue(newEntity, value, null);
                    }
                }

                return newEntity;
            }
        }

        /// <summary>
        /// Provides means of converting sequences of objects.
        /// </summary>
        private sealed class EnumerableConversionSource<TSource> : IEnumerableConversionSource<TSource>
        {
            private readonly IEnumerable<TSource> __source;

            public EnumerableConversionSource(IEnumerable<TSource> source)
            {
                if (source == null) throw new ArgumentNullException("source");

                __source = source;
            }

            public IEnumerableConversionSource<TInterface> As<TInterface>()
            {
                var @base = __source.Cast<TInterface>();

                return new EnumerableConversionSource<TInterface>(@base);
            }

            public IEnumerable<TResult> To<TResult>() where TResult : TSource, new()
            {
                EntityConversion.ValidateTypes(typeof(TSource), typeof(TResult));

                var properties = typeof(TSource).GetProperties();

                foreach (var oldEntity in __source)
                {
                    var newEntity = new TResult();

                    foreach (var prop in properties)
                    {
                        if (prop.CanRead && prop.CanWrite)
                        {
                            var value = prop.GetValue(oldEntity, null);

                            prop.SetValue(newEntity, value, null);
                        }
                    }

                    yield return newEntity;
                }
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates the types to ensure the conversion will succeed.
        /// </summary>
        private static void ValidateTypes(Type interfaceType, Type resultType)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException("Source must be an interface type.");

            if (resultType.IsInterface)
                throw new ArgumentException("Target type must be a concrete type, not an interface.");

            if (!interfaceType.IsAssignableFrom(resultType))
                throw new ArgumentException("Target type is not compatible with base (interface) type.");

            if (interfaceType == resultType)
                throw new ArgumentException("Entity and interface types must be different.");
        }

        #endregion
    }
}