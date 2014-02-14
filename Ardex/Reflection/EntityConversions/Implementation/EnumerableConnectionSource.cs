using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ardex.Reflection.EntityConversions.Implementation
{
    /// <summary>
    /// Provides means of converting sequences of objects.
    /// </summary>
    internal sealed class EnumerableConversionSource<TSource> : IEnumerableConversionSource<TSource>
    {
        private readonly IEnumerable<TSource> __source;

        public EnumerableConversionSource(IEnumerable<TSource> source)
        {
            if (source == null) throw new ArgumentNullException("source");

            __source = source;
        }

        public IEnumerableConversionSource<TInterface> As<TInterface>()
        {
            if (!typeof(TInterface).GetTypeInfo().IsInterface)
            {
                throw new InvalidOperationException(this.GetType().Name + ".As<TInterface> only supports interface types.");
            }

            var @base = __source.Cast<TInterface>();

            return new EnumerableConversionSource<TInterface>(@base);
        }

        public IEnumerable<TResult> To<TResult>() where TResult : TSource, new()
        {
            EntityConversion.ValidateTypes(typeof(TSource), typeof(TResult));

            var properties = typeof(TSource).GetRuntimeProperties();

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
}
