using System;

namespace Ardex
{
    /// <summary>
    /// Provides fast, lazy, thread-safe access to cached data.
    /// </summary>
    public class LazyCache<T> where T : class
    {
        /// <summary>
        /// Factory method used to fully
        /// regenerate the cache when required.
        /// </summary>
        private readonly Func<T> ValueFactory;

        /// <summary>
        /// Current value.
        /// </summary>
        private volatile Lazy<T> _value;

        /// <summary>
        /// Returns the inner cache initialising
        /// it in the process if necessary.
        /// Guaranteed to return a non-null value.
        /// </summary>
        public T Value
        {
            get { return _value.Value; }
        }

        /// <summary>
        /// Returns true if the cached data is current and ready to use.
        /// </summary>
        public bool IsValid
        {
            get {  return _value.IsValueCreated; }
        }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public LazyCache(Func<T> valueFactory)
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
            _value = new Lazy<T>(this.ValueFactory);
        }
    }
}

