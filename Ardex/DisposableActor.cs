using System;
using System.Threading;

namespace Ardex
{
    /// <summary>
    /// Performs the specified operation when Dispose is called.
    /// </summary>
    public class DisposableActor : IDisposable
    {
        private readonly Action Action;
        private int Disposed = 0;

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public DisposableActor(Action action)
        {
            if (action == null) throw new ArgumentNullException("action");

            this.Action = action;
        }

        /// <summary>
        /// Performs the action specified when this instance was created
        /// provided that Dispose has not already been called.
        /// </summary>
        public void Dispose()
        {
            var previouslyDisposed = Interlocked.Exchange(ref this.Disposed, 1);

            if (previouslyDisposed == 0)
            {
                this.Action();
            }
        }
    }
}

