using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync.ChangeTracking
{
    public class RepositoryChangeTracking<TEntity, TChangeHistory>
    {
        /// <summary>
        /// Gets the unique ID of this replica.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Gets the tracked repository.
        /// </summary>
        public SyncRepository<TEntity> Repository { get; private set; }

        /// <summary>
        /// Gets the change history repository which contains
        /// the change metadata for the tracked repository.
        /// </summary>
        public SyncRepository<TChangeHistory> ChangeHistory { get; private set; }

        /// <summary>
        /// Gets the object which is able to resolve
        /// unique ID values for the tracked entities.
        /// </summary>
        public UniqueIdMapping<TEntity> UniqueIdMapping { get; private set; }

        /// <summary>
        /// Indicates whether change tracking is turned on.
        /// </summary>
        public bool Enabled { get; internal set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public RepositoryChangeTracking(
            SyncID replicaID,
            SyncRepository<TEntity> entities,
            SyncRepository<TChangeHistory> changeHistory,
            UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            this.ReplicaID = replicaID;
            this.Repository = entities;
            this.ChangeHistory = changeHistory;
            this.UniqueIdMapping = uniqueIdMapping;

            // Defaults.
            this.Enabled = true;
        }
    }
}
