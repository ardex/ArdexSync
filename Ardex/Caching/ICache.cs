using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.Caching
{
    /// <summary>
    /// Common cache interface.
    /// </summary>
    public interface ICache<T>
    {
        /// <summary>
        /// Returns the cached value initialising it if necessary.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Returns true if the cached value is current and ready to use.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Invalidates the cache causing it to be rebuilt.
        /// </summary>
        void Invalidate();
    }
}