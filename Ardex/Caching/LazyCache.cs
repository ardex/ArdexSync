using System;
using System.Threading;

namespace Ardex.Caching
{
    /// <summary>
    /// Provides fast, lazy, thread-safe access to cached data.
    /// </summary>
    public class LazyCache<T> : ICache<T> where T : class
    {
        /// <summary>
        /// Factory method used to fully
        /// regenerate the cache when required.
        /// </summary>
        private readonly Func<T> ValueFactory;

        /// <summary>
        /// Current value.
        /// </summary>
        private Lazy<T> _lazy;

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
                // Bastardised version of "Interlocked Anything".
                T value;
                Lazy<T> currentLazy = _lazy, startLazy;

                do
                {
                    // Keep ref to _lazy as it was
                    // at the start of the operation.
                    startLazy = currentLazy;

                    // Generate new, or use cached value.
                    value = startLazy.Value;

                    // If the Lazy<T> reference has been swapped,
                    // a call to Invalidate() must have happened
                    // while the value was being generated.
                    // In that case, let's start again.
                    currentLazy = Volatile.Read(ref _lazy);
                }
                while (startLazy != currentLazy);

                return value;
            }
        }

        /// <summary>
        /// Returns true if the cached value is current and ready to use.
        /// </summary>
        public bool IsValid
        {
            get {  return _lazy.IsValueCreated; }
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
            Interlocked.Exchange(ref _lazy, new Lazy<T>(this.ValueFactory));
        }
    }

    ///// <summary>
    ///// Provides fast, lazy, thread-safe access to cached data.
    ///// </summary>
    //public class LazyCache2<T> where T : class
    //{
    //    /// <summary>
    //    /// Factory method used to fully
    //    /// regenerate the cache when required.
    //    /// </summary>
    //    private readonly Func<T> ValueFactory;

    //    /// <summary>
    //    /// Current value.
    //    /// </summary>
    //    //private Lazy<T> _lazy;

    //    private T _value;

    //    private readonly object ValueLock = new object();

    //    /// <summary>
    //    /// Returns the inner cache initialising
    //    /// it in the process if necessary.
    //    /// Guaranteed to return the latest value
    //    /// even if a call to Invalidate() is made
    //    /// while the value if being generated.
    //    /// </summary>
    //    public T Value
    //    {
    //        get
    //        {
    //            var value = _value;

    //            if (value != null)
    //                return value;

    //            // Ensure that no two threads
    //            // can create the value at
    //            // the same time.
    //            lock (this.ValueLock)
    //            {
    //                T currentValue = _value, startValue;

    //                if (currentValue != null)
    //                {
    //                    // Someone else has created the value.
    //                    return currentValue;
    //                }

    //                do
    //                {
    //                    // Keep ref to _value as it was
    //                    // at the start of the operation.
    //                    startValue = currentValue;

    //                    // Generate new value.
    //                    value = this.ValueFactory();

    //                    if (value == null)
    //                    {
    //                        throw new InvalidOperationException("It is illegal for ValueFactory to return null.");
    //                    }

    //                    // If the _value reference has been swapped,
    //                    // a call to Invalidate() must have happened
    //                    // while the value was being generated.
    //                    // In that case, let's start again.
    //                    currentValue = Interlocked.CompareExchange(ref _value, value, startValue);
    //                }
    //                while (startValue != currentValue);

    //                return value;
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Returns true if the cached data is current and ready to use.
    //    /// </summary>
    //    public bool IsValid
    //    {
    //        get { return _value != null; } // _lazy.IsValueCreated; }
    //    }

    //    /// <summary>
    //    /// Creates a new instance of the class.
    //    /// </summary>
    //    public LazyCache2(Func<T> valueFactory)
    //    {
    //        if (valueFactory == null) throw new ArgumentNullException("valueFactory");

    //        this.ValueFactory = valueFactory;

    //        this.Invalidate();
    //    }

    //    /// <summary>
    //    /// Invalidates the cache causing it to
    //    /// be rebuilt next time it is accessed.
    //    /// </summary>
    //    public void Invalidate()
    //    {
    //        Interlocked.Exchange(ref _value, null);
    //    }
    //}
}