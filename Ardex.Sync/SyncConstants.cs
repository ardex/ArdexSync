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
            #if DEBUG
            SyncConstants.DeadlockTimeout = TimeSpan.FromSeconds(15);
            #else
            SyncConstants.DeadlockTimeout = TimeSpan.FromSeconds(30);
            #endif

        }
    }
}