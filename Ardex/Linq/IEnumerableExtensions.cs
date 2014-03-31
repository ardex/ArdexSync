using System;
using System.Collections.Generic;

namespace Ardex.Linq
{
    /// <summary>
    /// Extensions for objects which implement generic
    /// or non-generic IEnumerable interface.
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Returns the index of the given element inside
        /// the collection or -1 if it cannot be found.
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> collection, T itemToSeek)
        {
            // Optimisation.
            var list = collection as IList<T>;

            if (list != null)
            {
                return list.IndexOf(itemToSeek);
            }

            // Seek.
            var index = 0;

            foreach (var item in collection)
            {
                if (object.Equals(item, itemToSeek))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        /// <summary>
        /// Breaks up the given sequence into smaller
        /// materialised sequences of the given size.
        /// </summary>
        public static IEnumerable<IList<TSource>> Chunkify<TSource>(this IEnumerable<TSource> collection, int chunkSize)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (chunkSize < 1) throw new ArgumentException("chunkSize");

            var chunk = default(List<TSource>);

            using (var enumerator = collection.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (chunk == null)
                    {
                        chunk = new List<TSource>(chunkSize);
                    }

                    chunk.Add(enumerator.Current);

                    if (chunk.Count == chunkSize)
                    {
                        yield return chunk;

                        chunk = null;
                    }
                }
            }

            if (chunk != null)
            {
                yield return chunk;
            }
        }
    }
}
