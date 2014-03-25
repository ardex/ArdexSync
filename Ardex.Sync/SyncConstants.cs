using System;

namespace Ardex.Sync
{
    /// <summary>
    /// Contains common constants.
    /// </summary>
    public static class SyncConstants
    {
        /// <summary>
        /// Timeout for lock acquisition after which a
        /// SyncDeadlockException is thrown (15 seconds).
        /// </summary>
        public static TimeSpan DeadlockTimeout { get; private set; }

        /// <summary>
        /// Static constuctor - sets defaults.
        /// </summary>
        static SyncConstants()
        {
            SyncConstants.DeadlockTimeout = TimeSpan.FromSeconds(15);
        }
    }
}