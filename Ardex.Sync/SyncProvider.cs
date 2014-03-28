using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Ardex.Reflection;
using Ardex.Sync.EntityMapping;

namespace Ardex.Sync
{
    /// <summary>
    /// Base class for merge synchronisation providers.
    /// </summary>
    public abstract class SyncProvider<TEntity, TKey, TVersion> : ISyncProvider<TEntity, TVersion>, IDisposable where TEntity : class
    {
        /// <summary>
        /// Unique ID of this replica.
        /// </summary>
        public SyncReplicaInfo ReplicaInfo { get; private set; }

        /// <summary>
        /// Repository which is being synchronised.
        /// </summary>
        public ISyncRepository<TEntity> Repository { get; private set; }

        /// <summary>
        /// Entity primary key / unique identifier mapping.
        /// </summary>
        public SyncEntityKeyMapping<TEntity, TKey> EntityKeyMapping { get; private set; }

        /// <summary>
        /// Gets or sets the entity change reconciler used
        /// by this sync provider to apply changes.
        /// </summary>
        public TypeMapping<TEntity> EntityTypeMapping { get; set; }

        /// <summary>
        /// Gets or sets the delegate applied to remote entities
        /// before they are inserted into the local repository.
        /// </summary>
        public SyncEntityAction<TEntity> PreInsertProcessing { get; set; }

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
            ISyncRepository<TEntity> repository,
            SyncEntityKeyMapping<TEntity, TKey> entityKeyMapping)
        {
            this.ReplicaInfo = replicaInfo;
            this.Repository = repository;
            this.EntityKeyMapping = entityKeyMapping;

            // Defaults.
            this.EntityTypeMapping = new TypeMapping<TEntity>();
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
            var sw = Stopwatch.StartNew();

            try
            {
            if (this.Repository == null)
            {
                throw new NotSupportedException(
                    "AcceptChanges cannot be called on a SyncProvider which is not backed by a repository.");
            }

            if (remoteDelta.Changes.Length == 0)
            {
                // No changes needed to be synchronised.
                #if PERF_DIAGNOSTICS
                    Debug.WriteLine("SyncProvider<{0}>.AcceptChanges: no changes needed to accept.", typeof(TEntity).Name);
                #endif

                return new SyncResult();
            }

            // Critical region: protected with exclusive lock.
            using (this.Repository.SyncLock.WriteLock())
            {
                remoteDelta = this.ResolveConflicts(remoteDelta);

                var type = typeof(TEntity);
                    var inserts = new List<TEntity>();
                    var updates = new List<TEntity>();
                    var deletes = new List<TEntity>();

                // Materialise repository entities, but only if necessary.
                var existingEntities = new Lazy<List<TEntity>>(this.Repository.ToList);

                // We need to ensure that all changes are processed in such
                // an order that if we fail, we'll be able to resume later.
                foreach (var change in remoteDelta.Changes.OrderBy(c => c.Version, this.VersionComparer))
                {
                    var changeKey = this.EntityKeyMapping(change.Entity);

                    if (object.Equals(changeKey, default(TKey)))
                    {
                        throw new InvalidOperationException(
                            "ChangeKey cannot be equal to the default value of TKey."
                        );
                    }

                    var existingEntity = existingEntities.Value.FirstOrDefault(e => object.Equals(changeKey, this.EntityKeyMapping(e)));

                    if (existingEntity == null)
                    {
                        // Not found.
                        if (this.PreInsertProcessing != null)
                        {
                            this.PreInsertProcessing(change.Entity);
                        }

                        this.Repository.UntrackedInsert(change.Entity);
                        existingEntities.Value.Add(change.Entity);
                        inserts.Add(change.Entity);
                    }
                    else
                    {
                        // Found.
                        var changeCount = this.EntityTypeMapping.CopyValues(change.Entity, existingEntity);

                        if (changeCount != 0)
                        {
                            this.Repository.UntrackedUpdate(existingEntity);
                            updates.Add(existingEntity);
                        }
                    }

                    // Write remote change history entry to local change history.
                    this.WriteRemoteVersion(change);
                }

                Debug.WriteLine("{0} applied {1} {2} inserts originating at {3}.", this.ReplicaInfo, inserts.Count, type.Name, remoteDelta.ReplicaInfo);
                Debug.WriteLine("{0} applied {1} {2} updates originating at {3}.", this.ReplicaInfo, updates.Count, type.Name, remoteDelta.ReplicaInfo);
                Debug.WriteLine("{0} applied {1} {2} deletes originating at {3}.", this.ReplicaInfo, deletes.Count, type.Name, remoteDelta.ReplicaInfo);

                var result = new SyncResult(inserts, updates, deletes);

                // Perform metadata cleanup.
                if (this.CleanUpMetadata)
                {
                    this.CleanUpSyncMetadata(remoteDelta.Changes);
                }

                return result;
                }
            }
            finally
            {
                Debug.WriteLine("{0}.AcceptChanges() completed in {1:0.###} seconds.", this.GetType().Name, (float)sw.ElapsedMilliseconds / 1000);
            }
        }

        /// <summary>
        /// Resolves conflicts and returns the original
        /// remote delta instance, or a new delta instance
        /// with changes filtered according to the conflict
        /// resolution strategy.
        /// </summary>
        protected virtual SyncDelta<TEntity, TVersion> ResolveConflicts(SyncDelta<TEntity, TVersion> remoteDelta)
        {
            // Detect conflicts.
            var myDelta = this.ResolveDelta(remoteDelta.Anchor);

            var conflicts = myDelta.Changes.Join(
                remoteDelta.Changes,
                c => this.EntityKeyMapping(c.Entity),
                c => this.EntityKeyMapping(c.Entity),
                (local, remote) => new SyncConflict<TEntity, TVersion>(local, remote)
            );

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
                this.EntityKeyMapping = null;
                this.Repository = null;
            }

            _disposed = true;
        }

        #endregion
    }
}