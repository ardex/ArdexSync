using System.Collections.Generic;

namespace Ardex.EntityConversions
{
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
}
