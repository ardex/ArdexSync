using System;

using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync.ChangeTracking
{
    /// <summary>
    /// Facilitates change history installation.
    /// </summary>
    public class ChangeTrackingFactory
    {
        /// <summary>
        /// Unique ID of the replica which tracks the repository changes.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public ChangeTrackingFactory(SyncID replicaID)
        {
            this.ReplicaID = replicaID;
        }

        /// <summary>
        /// Creates links necessary for change tracking to work with
        /// a change history repository which tracks a single article.
        /// </summary>
        public void InstallExclusiveChangeTracking<TEntity>(
            ISyncRepositoryWithChangeTracking<TEntity> repository, ISyncRepository<IChangeHistory> changeHistory, UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            if (repository.TrackInsert != null ||
                repository.TrackUpdate != null ||
                repository.TrackDelete != null)
            {
                throw new InvalidOperationException(
                    "Unable to install change history link: repository already provisioned for change tracking.");
            }

            repository.TrackInsert = entity => ChangeTrackingUtil.WriteLocalChangeHistory(
                changeHistory, entity, this.ReplicaID, uniqueIdMapping, ChangeHistoryAction.Insert);

            repository.TrackUpdate = entity => ChangeTrackingUtil.WriteLocalChangeHistory(
                changeHistory, entity, this.ReplicaID, uniqueIdMapping, ChangeHistoryAction.Update);

            // We are intentionally leaving out the delete.
            // It's up to the repository to detect that it's
            // not hooked up, and throw the exception.
            //repository.TrackedDelete = entity => { throw new NotImplementedException(); };
        }

        /// <summary>
        /// Creates links necessary for change tracking to work with
        /// a change history repository which tracks multiple articles.
        /// </summary>
        public void InstallSharedChangeTracking<TEntitiy>(
            ISyncRepositoryWithChangeTracking<TEntitiy> repository, ISyncRepository<ISharedChangeHistory> changeHistory, UniqueIdMapping<TEntitiy> uniqueIdMapping)
        {
            throw new NotImplementedException();
        }
    }
}
