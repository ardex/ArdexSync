using System;
using System.Threading;

namespace Ardex
{
    /// <summary>
    /// Commonly used IDisposable factory methods.
    /// </summary>
    public static class Disposables
    {
        /// <summary>
        /// Returns a simple non-thread-safe disposable
        /// which invokes the given action any time
        /// a call to Dispose is made.
        /// </summary>
        public static IDisposable Multi(Action disposeAction)
        {
            if (disposeAction == null) throw new ArgumentException("disposeAction");

            return new SimpleDisposable(disposeAction);
        }

        /// <summary>
        /// Returns a thread-safe disposable which
        /// guarantees that the Dispose action is
        /// executed at most once.
        /// </summary>
        public static IDisposable Once(Action disposeAction)
        {
            return new DisposableActor(disposeAction);
        }

        /// <summary>
        /// Returns a disposable which does nothing when disposed.
        /// </summary>
        public static IDisposable Null
        {
            get { return new NullDisposable(); }
        }

        private class SimpleDisposable : IDisposable
        {
            private readonly Action DisposeAction;

            public SimpleDisposable(Action disposeAction)
            {
                this.DisposeAction = disposeAction;
            }

            public void Dispose()
            {
                this.DisposeAction();
            }
        }

        /// <summary>
        /// Performs the specified operation when Dispose is called.
        /// </summary>
        private class DisposableActor : IDisposable
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

        private struct NullDisposable : IDisposable
        {
            public void Dispose()
            {
                // Do nothing.
            }
        }
    }
}