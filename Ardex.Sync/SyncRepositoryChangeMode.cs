namespace Ardex.Sync
{
    /// <summary>
    /// Provides additional information about the change.
    /// </summary>
    public enum SyncRepositoryChangeMode
    {
        /// <summary>
        /// The change is untracked and will raise the
        /// TrackedChange event in addition to the
        /// usual EntityInserted/Updated/Deleted event.
        /// </summary>
        Tracked,

        /// <summary>
        /// The change is untracked and will raise the
        /// UntrackedChange event in addition to the
        /// usual EntityInserted/Updated/Deleted event.
        /// </summary>
        Untracked
    }
}