using System;
using System.Reflection;

namespace Ardex.EntityConversions.Implementation
{
    /// <summary>
    /// Provides means of converting single objects.
    /// </summary>
    internal sealed class SingleConversionSource<TSource> : ISingleConversionSource<TSource>
    {
        private readonly TSource __source;

        public SingleConversionSource(TSource source)
        {
            if (source == null) throw new ArgumentNullException("source");

            __source = source;
        }

        public ISingleConversionSource<TInterface> As<TInterface>()
        {
            if (!typeof(TInterface).GetTypeInfo().IsInterface)
            {
                throw new InvalidOperationException(this.GetType().Name + ".As<TInterface> only supports interface types.");
            }

            var @base = (TInterface)(object)__source;

            return new SingleConversionSource<TInterface>(@base);
        }

        public TResult To<TResult>() where TResult : TSource, new()
        {
            var newEntity = new TResult();

            this.Fill(newEntity);

            return newEntity;
        }

        public void Fill<TResult>(TResult newEntity) where TResult : TSource
        {
            EntityConversion.ValidateTypes(typeof(TSource), typeof(TResult));

            var properties = typeof(TSource).GetRuntimeProperties();

            foreach (var prop in properties)
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    var value = prop.GetValue(__source, null);

                    prop.SetValue(newEntity, value, null);
                }
            }
        }
    }
}
