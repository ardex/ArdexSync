using System;
using System.Threading;

using Ardex.Threading;

namespace Ardex.Caching
{
    /// <summary>
    /// Provides fast, lazy, thread-safe access to cached data.
    /// </summary>
    public class InterlockedCache<T> : ICache<T> where T : class
    {
        /// <summary>
        /// Factory method used to fully
        /// regenerate the cache when required.
        /// </summary>
        private readonly Func<T> ValueFactory;

        private T _value;

        /// <summary>
        /// Returns the cached value initialising it
        /// using the factory delegate if necessary.
        /// Guaranteed to return the latest value
        /// even if a call to Invalidate() is made
        /// while the value is being generated.
        /// </summary>
        public T Value
        {
            get
            {
                return Atomic.Transform(
                    ref _value,
                    this.ValueFactory,
                    (value, valueFactory) => value ?? valueFactory()
                );
            }
        }

        /// <summary>
        /// Returns true if the cached value is current and ready to use.
        /// </summary>
        public bool IsValid
        {
            get { return _value != null; }
        }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public InterlockedCache(Func<T> valueFactory)
        {
            if (valueFactory == null) throw new ArgumentNullException("valueFactory");

            this.ValueFactory = valueFactory;

            this.Invalidate();
        }

        /// <summary>
        /// Invalidates the cache causing it to
        /// be rebuilt next time it is accessed.
        /// </summary>
        public void Invalidate()
        {
            Interlocked.Exchange(ref _value, null);
        }
    }
}