using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Ardex.Threading.Async
{
    /// <summary>
    /// Provides support for asynchronous lazy initialisation.
    /// This type is fully thread-safe.
    /// </summary>
    /// <typeparam name="T">The type of object that is being asynchronously initialised.</typeparam>
    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="valueFactory">The delegate that is invoked on a background thread to produce the value when it is needed.</param>
        public AsyncLazy(Func<T> valueFactory) : base(() => Task.Run(valueFactory))
        {

        }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="taskFactory">The asynchronous delegate that is invoked on a background thread to produce the value when it is needed.</param> 
        public AsyncLazy(Func<Task<T>> taskFactory) : base(() => Task.Run(taskFactory))
        {

        }

        /// <summary>
        /// Gets the awaiter (thus making this type awaitable).
        /// </summary>
        public TaskAwaiter<T> GetAwaiter()
        {
            return this.Value.GetAwaiter();
        }

        /// <summary>
        /// Starts the asynchronous initialisation,
        /// if it has not already started.
        /// </summary>
        public void Start()
        {
            var _ = this.Value;
        }
    }
}

