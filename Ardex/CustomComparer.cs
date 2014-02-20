/*
using System;
using System.Collections.Generic;

namespace Ardex
{
    /// <summary>
    /// IComparer implementation which uses a Comparison delegate.
    /// </summary>
    public class CustomComparer<T> : Comparer<T>
    {
        private readonly Comparison<T> __comparison;

        /// <summary>
        /// Creates a new instance of CustomComparer.
        /// </summary>
        public CustomComparer(Comparison<T> comparison)
        {
            if (comparison == null) throw new ArgumentNullException("comparison");

            __comparison = comparison;
        }

        /// <summary>
        /// Performs a comparison of two objects
        /// of the same type and returns a value
        /// indicating whether one object is less
        /// than, equal to, or greater than the other.
        /// </summary>
        public override int Compare(T x, T y)
        {
            return __comparison(x, y);
        }
    }
}
*/