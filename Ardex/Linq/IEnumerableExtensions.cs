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
    }
}

