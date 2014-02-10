using System;

namespace Ardex.Sync
{
    /// <summary>
    /// Exception thrown when a merge conflict is detected.
    /// </summary>
    public class SyncConflictException : Exception
    {
        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public SyncConflictException(string message) : base(message) { }
    }
}