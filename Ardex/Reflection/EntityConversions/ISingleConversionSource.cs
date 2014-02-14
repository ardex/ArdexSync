namespace Ardex.Reflection.EntityConversions
{
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

        /// <summary>
        /// Performs a shallow copy of property values
        /// from the source to the given instance.
        /// </summary>
        void Fill<TResult>(TResult newEntity) where TResult : TSource;
    }
}
