using System;

namespace Ardex.Sync
{
    /// <summary>
    /// Exception raised when a deadlock is detected while
    /// trying to obtain lock on a sync repository.
    /// </summary>
    public class SyncDeadlockException : Exception
    {
        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public SyncDeadlockException()
            : base("A deadlock was detected while trying to obtain lock on a sync repository.") { }
    }
}