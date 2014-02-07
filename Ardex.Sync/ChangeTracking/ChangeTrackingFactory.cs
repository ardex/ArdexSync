using System;
using System.Linq;

using Ardex.Sync.Providers.ChangeBased;
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
        /// One change history repository is used exclusively by one data repository.
        /// </summary>
        public RepositoryChangeTracking<TEntity, IChangeHistory> Exclusive<TEntity>(
            SyncRepository<TEntity> repository,
            SyncRepository<IChangeHistory> changeHistoryRepository,
            UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            var newChangeTracking = new RepositoryChangeTracking<TEntity, IChangeHistory>(
                this.ReplicaID, repository, changeHistoryRepository, uniqueIdMapping);

            this.HookUpEvents(
                newChangeTracking,
                (changeTracking, entity, action) =>
                {
                    if (changeTracking.Enabled)
                    {
                        var changeHistory = (ISyncRepository<IChangeHistory>)changeTracking.ChangeHistory;

                        changeHistory.ObtainExclusiveLock();

                        try
                        {
                            var ch = (IChangeHistory)new ChangeHistory();

                            // Resolve pk.
                            ch.ChangeHistoryID = changeHistory.Unlocked
                                .Select(c => c.ChangeHistoryID)
                                .DefaultIfEmpty()
                                .Max() + 1;


                            ch.Action = action;
                            ch.ReplicaID = this.ReplicaID;
                            ch.UniqueID = uniqueIdMapping.Get(entity);

                            // Resolve timestamp.
                            var timestamp = changeHistory.Unlocked
                                .Where(c => c.ReplicaID == this.ReplicaID)
                                .Select(c => c.Timestamp)
                                .DefaultIfEmpty()
                                .Max();

                            ch.Timestamp = (timestamp == null ? new Timestamp(1) : ++timestamp);

                            changeHistory.Unlocked.Insert(ch);
                        }
                        finally
                        {
                            changeHistory.ReleaseExclusiveLock();
                        }
                    }
                });

            return newChangeTracking;
        }

        public void HookUpEvents<TEntity, TChangeHistory>(
            RepositoryChangeTracking<TEntity, TChangeHistory> changeTracking,
            Action<RepositoryChangeTracking<TEntity, TChangeHistory>, TEntity, ChangeHistoryAction> localChangeHandler)
        {
            changeTracking.Repository.EntityInserted += e => localChangeHandler(changeTracking, e, ChangeHistoryAction.Insert);
            changeTracking.Repository.EntityUpdated += e => localChangeHandler(changeTracking, e, ChangeHistoryAction.Update);
            changeTracking.Repository.EntityDeleted += e => localChangeHandler(changeTracking, e, ChangeHistoryAction.Delete);
        }
    }
}
