using System;
using System.Collections.Generic;

namespace Ardex
{
    /// <summary>
    /// IEqualityComparer implementation which uses a delegate.
    /// </summary>
    public class CustomEqualityComparer<T> : EqualityComparer<T>
    {
        private readonly Func<T, T, bool> __comparison;

        /// <summary>
        /// Creates a new instance of CustomComparer.
        /// </summary>
        public CustomEqualityComparer(Func<T, T, bool> comparison)
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
        public override bool Equals(T x, T y)
        {
            return __comparison(x, y);
        }

        public override int GetHashCode(T obj)
        {
            throw new NotImplementedException();
        }
    }
}
