﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Ardex.Sync.EntityMapping;

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
        public SyncReplicaInfo ReplicaInfo { get; private set; }

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
        public virtual SyncConflictStrategy ConflictStrategy { get; set; }

        /// <summary>
        /// If true, the change history will be kept minimal
        /// by truncating all but the last entry at the end
        /// of the sync operation. Should only ever be enabled
        /// on the client in a client-server sync topology.
        /// The default is false.
        /// </summary>
        public virtual bool CleanUpMetadata { get; set; }

        /// <summary>
        /// Comparer responsible for comparing timestamps
        /// and other versioning data structures.
        /// </summary>
        protected abstract IComparer<TVersion> VersionComparer { get; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        protected SyncProvider(
            SyncReplicaInfo replicaInfo,
            SyncRepository<TEntity> repository,
            UniqueIdMapping<TEntity> entityIdMapping)
        {
            this.ReplicaInfo = replicaInfo;
            this.Repository = repository;
            this.EntityIdMapping = entityIdMapping;
        }

        #region Abstract methods

        /// <summary>
        /// Retrieves the last anchor containing
        /// this replica's latest change knowledge.
        /// It is used by the other side in order to detect
        /// the change delta that needs to be transferred
        /// and to detect and resolve conflicts.
        /// </summary>
        public abstract SyncAnchor<TVersion> LastAnchor();
        
        /// <summary>
        /// Resolves the changes made since the last reported anchor.
        /// </summary>
        public abstract SyncDelta<TEntity, TVersion> ResolveDelta(SyncAnchor<TVersion> remoteAnchor);

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
        public SyncResult AcceptChanges(SyncDelta<TEntity, TVersion> remoteDelta)
        {
            if (this.Repository == null)
            {
                throw new NotSupportedException(
                    "AcceptChanges cannot be called on a SyncProvider which is not backed by a repository.");
            }

            // Critical region: protected with exclusive lock.
            this.Repository.Lock.EnterWriteLock();

            try
            {
                remoteDelta = this.ResolveConflicts(remoteDelta);

                var type = typeof(TEntity);
                var inserts = new List<object>();
                var updates = new List<object>();
                var deletes = new List<object>();

                // We need to ensure that all changes are processed in such
                // an order that if we fail, we'll be able to resume later.
                foreach (var change in remoteDelta.Changes.OrderBy(c => c.Version, this.VersionComparer))
                {
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
                                this.Repository.UntrackedUpdate(existingEntity);
                                updates.Add(existingEntity);
                            }

                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        this.Repository.UntrackedInsert(change.Entity);
                        inserts.Add(change);
                    }

                    // Write remote change history entry to local change history.
                    this.WriteRemoteVersion(change);
                }

                Debug.Print("{0} applied {1} {2} inserts originating at {3}.", this.ReplicaInfo, inserts.Count, type.Name, remoteDelta.ReplicaInfo);
                Debug.Print("{0} applied {1} {2} updates originating at {3}.", this.ReplicaInfo, updates.Count, type.Name, remoteDelta.ReplicaInfo);
                Debug.Print("{0} applied {1} {2} deletes originating at {3}.", this.ReplicaInfo, deletes.Count, type.Name, remoteDelta.ReplicaInfo);

                var result = new SyncResult(inserts, updates, deletes);

                // Perform metadata cleanup.
                if (this.CleanUpMetadata)
                {
                    this.CleanUpSyncMetadata(remoteDelta.Changes);
                }

                return result;
            }
            finally
            {
                this.Repository.Lock.ExitWriteLock();
            }
        }

        protected virtual SyncDelta<TEntity, TVersion> ResolveConflicts(SyncDelta<TEntity, TVersion> remoteDelta)
        {
            // Detect conflicts.
            var myDelta = this.ResolveDelta(remoteDelta.Anchor);

            var conflicts = myDelta.Changes.Join(
                remoteDelta.Changes,
                c => this.EntityIdMapping.Get(c.Entity),
                c => this.EntityIdMapping.Get(c.Entity),
                (local, remote) => new SyncConflict<TEntity, TVersion>(local, remote));

            // Default: fail if merge conflicts detected.
            if (this.ConflictStrategy == SyncConflictStrategy.Fail)
            {
                if (conflicts.Any())
                {
                    throw new SyncConflictException("Merge conflict detected.");
                }

                return remoteDelta;
            }

            // Ignore changes which conflict with ours.
            if (this.ConflictStrategy == SyncConflictStrategy.Winner)
            {
                var ignoredChanges = conflicts.Select(c => c.Remote);

                foreach (var ignoredChange in ignoredChanges)
                {
                    this.WriteRemoteVersion(ignoredChange);
                }

                // Discard other replica's changes. Ours are better.
                return SyncDelta.Create(remoteDelta.ReplicaInfo, remoteDelta.Anchor, remoteDelta.Changes.Except(ignoredChanges));
            }

            // Do nothing: allow our changes to be overwritten.
            if (this.ConflictStrategy == SyncConflictStrategy.Loser)
            {
                return remoteDelta;
            }

            throw new InvalidOperationException("Unknown ConflictStrategy.");
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

        #region IDisposable implementation

        private bool _disposed;

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                this.EntityIdMapping = null;
                this.Repository = null;
            }

            _disposed = true;
        }

        #endregion
    }
}
