namespace Ardex.Sync
{
    /// <summary>
    /// Contains information about a merge sync conflict.
    /// </summary>
    public class SyncConflict<TEntity, TVersion>
    {
        /// <summary>
        /// Local entity involved in the conflict, and its version.
        /// </summary>
        public SyncEntityVersion<TEntity, TVersion> Local { get; private set; }

        /// <summary>
        /// Remote entity involved in the conflict, and its version.
        /// </summary>
        public SyncEntityVersion<TEntity, TVersion> Remote { get; private set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public SyncConflict(SyncEntityVersion<TEntity, TVersion> local, SyncEntityVersion<TEntity, TVersion> remote)
        {
            this.Local = local;
            this.Remote = remote;
        }
    }
}
