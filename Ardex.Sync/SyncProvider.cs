using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync
{
    /// <summary>
    /// Base class for merge synchronisation providers.
    /// </summary>
    public abstract class SyncProvider<TEntity, TVersion> : ISyncProvider<TEntity, TVersion>
    {
        /// <summary>
        /// Unique ID of this replica.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Repository which is being synchronised.
        /// </summary>
        public SyncRepository<TEntity> Repository { get; private set; }

        /// <summary>
        /// Entity primary key / unique identifier mapping.
        /// </summary>
        public UniqueIdMapping<TEntity> EntityIdMapping { get; private set; }

        /// <summary>
        /// Conflict resolution strategy used by this provider.
        /// </summary>
        public SyncConflictStrategy ConflictStrategy { get; set; }

        /// <summary>
        /// If true, the change history will be kept minimal
        /// by truncating all but the last entry at the end
        /// of the sync operation. Should only ever be enabled
        /// on the client in a client-server sync topology.
        /// The default is false.
        /// </summary>
        public bool CleanUpMetadata { get; set; }

        /// <summary>
        /// Comparer responsible for comparing timestamps
        /// and other versioning data structures.
        /// </summary>
        protected abstract IComparer<TVersion> VersionComparer { get; }

        /// <summary>
        /// When overridden in a derived class, enables temporary
        /// suppression of the change tracking functionality for the
        /// purpose of writing custom change entries during the sync.
        /// </summary>
        protected bool ChangeTrackingEnabled { get; set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        protected SyncProvider(
            SyncID replicaID,
            SyncRepository<TEntity> repository,
            UniqueIdMapping<TEntity> entityIdMapping)
        {
            this.ReplicaID = replicaID;
            this.Repository = repository;
            this.EntityIdMapping = entityIdMapping;

            // Defaults.
            this.ChangeTrackingEnabled = true;
        }

        #region Abstract methods

        /// <summary>
        /// Retrieves the last anchor containing
        /// this replica's latest change knowledge.
        /// It is used by the other side in order to detect
        /// the change delta that needs to be transferred
        /// and to detect and resolve conflicts.
        /// </summary>
        public abstract Dictionary<SyncID, TVersion> LastAnchor();
        
        /// <summary>
        /// Resolves the changes made since the last reported anchor.
        /// </summary>
        public abstract SyncDelta<TEntity, TVersion> ResolveDelta(Dictionary<SyncID, TVersion> anchor, CancellationToken ct);

        /// <summary>
        /// When overridden in a derived class, performs
        /// post-sync metadata (change history) cleanup.
        /// </summary>
        protected abstract void CleanUpSyncMetadata(IEnumerable<SyncEntityVersion<TEntity, TVersion>> appliedChanges);

        /// <summary>
        /// When overridden in a derived class, applies the
        /// given remote change entry locally if necessary.
        /// </summary>
        protected abstract void WriteRemoteVersion(SyncEntityVersion<TEntity, TVersion> versionInfo);

        #endregion

        /// <summary>
        /// Accepts the changes as reported by the given node.
        /// </summary>
        public SyncResult AcceptChanges(SyncID sourceReplicaID, SyncDelta<TEntity, TVersion> delta, CancellationToken ct)
        {
            // Critical region: protected with exclusive lock.
            this.Repository.Lock.EnterWriteLock();

            try
            {
                // Temporarily suspend change tracking.
                this.ChangeTrackingEnabled = false;

                try
                {
                    // Materialise changes.
                    var changes = delta.Changes.ToArray().AsEnumerable();

                    // Detect conflicts.
                    var myDelta = this.ResolveDelta(delta.Anchor, ct);

                    var conflicts = myDelta.Changes.Join(
                        changes,
                        c => this.EntityIdMapping.Get(c.Entity),
                        c => this.EntityIdMapping.Get(c.Entity),
                        (local, remote) => SyncConflict.Create(local, remote));

                    // Resolve conflicts.
                    if (this.ConflictStrategy == SyncConflictStrategy.Fail)
                    {
                        if (conflicts.Any())
                        {
                            throw new InvalidOperationException("Merge conflict detected.");
                        }
                    }
                    else if (this.ConflictStrategy == SyncConflictStrategy.Winner)
                    {
                        var ignoredChanges = conflicts.Select(c => c.Remote);

                        foreach (var ignoredChange in ignoredChanges)
                        {
                            this.WriteRemoteVersion(ignoredChange);
                        }

                        // Discard other replica's changes. Ours are better.
                        changes = changes.Except(ignoredChanges);
                    }
                    else if (this.ConflictStrategy == SyncConflictStrategy.Loser)
                    {
                        // We'll just pretend that nothing happened.
                    }

                    var type = typeof(TEntity);
                    var inserts = new List<object>();
                    var updates = new List<object>();
                    var deletes = new List<object>();

                    // We need to ensure that all changes are processed in such
                    // an order that if we fail, we'll be able to resume later.
                    foreach (var change in changes.OrderBy(c => c.Version, this.VersionComparer))
                    {
                        ct.ThrowIfCancellationRequested();

                        var changeUniqueID = this.EntityIdMapping.Get(change.Entity);
                        var found = false;

                        foreach (var existingEntity in this.Repository)
                        {
                            if (changeUniqueID == this.EntityIdMapping.Get(existingEntity))
                            {
                                // Found.
                                var changeCount = this.ApplyDataChange(existingEntity, change.Entity);

                                if (changeCount != 0)
                                {
                                    this.Repository.Update(existingEntity);
                                    updates.Add(existingEntity);
                                }

                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            this.Repository.Insert(change.Entity);
                            inserts.Add(change);
                        }

                        // Write remote change history entry to local change history.
                        this.WriteRemoteVersion(change);
                    }

                    ct.ThrowIfCancellationRequested();

                    Debug.Print("{0} applied {1} {2} inserts originating at {3}.", this.ReplicaID, inserts.Count, type.Name, sourceReplicaID);
                    Debug.Print("{0} applied {1} {2} updates originating at {3}.", this.ReplicaID, updates.Count, type.Name, sourceReplicaID);
                    Debug.Print("{0} applied {1} {2} deletes originating at {3}.", this.ReplicaID, deletes.Count, type.Name, sourceReplicaID);

                    var result = new SyncResult(inserts, updates, deletes);

                    // Perform metadata cleanup.
                    this.CleanUpSyncMetadata(changes);

                    return result;
                }
                finally
                {
                    this.ChangeTrackingEnabled = true;
                }
            }
            finally
            {
                this.Repository.Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Reconciles the differences where necessary,
        /// and returns the number of changes applied.
        /// </summary>
        protected virtual int ApplyDataChange(TEntity original, TEntity modified)
        {
            var changeCount = 0;
            var type = typeof(TEntity);
            var props = type.GetProperties();

            foreach (var prop in props)
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    var oldValue = prop.GetValue(original);
                    var newValue = prop.GetValue(modified);

                    if (!object.Equals(oldValue, newValue))
                    {
                        prop.SetValue(original, newValue);
                        changeCount++;
                    }
                }
            }

            return changeCount;
        }
    }
}
