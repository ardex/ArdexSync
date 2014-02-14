using System;
using System.Reflection;

namespace Ardex.Reflection.EntityConversions.Implementation
{
    /// <summary>
    /// Provides means of converting single objects.
    /// </summary>
    internal sealed class SingleConversionSource<TSource> : ISingleConversionSource<TSource>
    {
        private static readonly Lazy<TypeMapping<TSource>> __mapping = new Lazy<TypeMapping<TSource>>();

        private static TypeMapping<TSource> Mapping
        {
            get
            {
                return __mapping.Value;
            }
        }

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
            SingleConversionSource<TSource>.Mapping.CopyValues(__source, newEntity);
        }
    }
}
