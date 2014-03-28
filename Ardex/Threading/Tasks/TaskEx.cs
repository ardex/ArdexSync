using System;
using System.Threading.Tasks;

namespace Ardex.Threading.Tasks
{
    /// <summary>
    /// Task extensions.
    /// </summary>
    public static class TaskEx
    {
        /// <summary>
        /// Used to suppress compiler warnings
        /// regarding unawaited tasks.
        /// Use with caution.
        /// </summary>
        public static void AsVoid(this Task task)
        {
            // Does nothing.
        }

        /// <summary>
        /// Schedules a synchronous continuation on
        /// the original task which will cause any
        /// exceptions of the given type
        /// to be handled so as not to propagate
        /// them when the task is awaited.
        /// Will throw if any of the exceptions
        /// thrown by the original task do not
        /// match the given type.
        /// Returns the scheduled continuation.
        /// </summary>
        public static Task HandleExceptions<TException>(this Task task) where TException : Exception
        {
            return task.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    t.Exception.Handle(ex => ex is TException);
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Schedules a synchronous continuation on
        /// the original task which will cause any
        /// exceptions matching the given predicate
        /// to be handled so as not to propagate
        /// them when the task is awaited.
        /// Will throw if any of the exceptions
        /// thrown by the original task do not
        /// match the given predicate.
        /// Returns the scheduled continuation.
        /// </summary>
        public static Task HandleExceptions(this Task task, Func<Exception, bool> predicate)
        {
            return task.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    t.Exception.Handle(predicate);
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Schedules a synchronous continuation to the given
        /// task which will return true once the original
        /// task runs to completion, or false if it fails
        /// or gets cancelled. Marks any exceptions
        /// thrown by the original task as observed.
        /// </summary>
        public static Task<bool> RanToCompletion(this Task task)
        {
            return task.ContinueWith(t =>
            {
                switch (t.Status)
                {
                    case TaskStatus.RanToCompletion : return true;
                    case TaskStatus.Faulted : return false;
                    case TaskStatus.Canceled : return false;
                    default : throw new InvalidOperationException("Unexpected task state in RanToCompletion().");
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}

