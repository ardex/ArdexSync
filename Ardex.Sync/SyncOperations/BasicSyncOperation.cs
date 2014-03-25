using System.Diagnostics;
using System.Threading;

namespace Ardex.Sync.SyncOperations
{
    /// <summary>
    /// Basic sync operation implementation.
    /// </summary>
    public class BasicSyncOperation<TEntity, TVersion> : SyncOperation
    {
        #region Transformation methods

        public virtual FilteredSyncOperation<TEntity, TVersion> Filtered(SyncFilter<TEntity, TVersion> filter)
        {
            return new FilteredSyncOperation<TEntity, TVersion>(this.Source, this.Target, filter);
        }

        #endregion

        /// <summary>
        /// Sync operation source or provider.
        /// Resolves the change delta in a differential sync operation
        /// based on anchor info provided by the sync target.
        /// </summary>
        public ISyncProvider<TEntity, TVersion> Source { get; private set; }

        /// <summary>
        /// Sync operation target or provider.
        /// Produces anchor info for the
        /// source and accepts changes.
        /// </summary>
        public ISyncProvider<TEntity, TVersion> Target { get; private set; }

        /// <summary>
        /// Creates a new SyncOperation instance.
        /// Consider using SyncOperation.Create for convenience.
        /// </summary>
        public BasicSyncOperation(ISyncProvider<TEntity, TVersion> source, ISyncProvider<TEntity, TVersion> target)
        {
            this.Source = source;
            this.Target = target;
        }

        /// <summary>
        /// Differential synchronisation implementation.
        /// </summary>
        protected override SyncResult SynchroniseDiff(CancellationToken ct)
        {
            var remoteAnchor = this.LastAnchor();
            ct.ThrowIfCancellationRequested();

            var delta = this.ResolveDelta(remoteAnchor);
            ct.ThrowIfCancellationRequested();

            if (delta.Changes.Length == 0)
            {
                #if PERF_DIAGNOSTICS
                    Debug.WriteLine("SyncOperation<{0}>.SynchroniseDiff: no changes needed to be synchronised.", typeof(TEntity).Name);
                #endif

                // No need to call AcceptChanges:
                // there are no changes to apply.
                return new SyncResult();
            }

            var result = this.AcceptChanges(delta);

            #if PERF_DIAGNOSTICS
                Debug.WriteLine("SyncOperation<{0}>.SynchroniseDiff: {1} changes applied by AcceptChanges.", typeof(TEntity).Name, result.ChangeCount);
            #endif

            return result;
        }

        protected virtual SyncAnchor<TVersion> LastAnchor()
        {
            return this.Target.LastAnchor();
        }

        protected virtual SyncDelta<TEntity, TVersion> ResolveDelta(SyncAnchor<TVersion> remoteAnchor)
        {
            return this.Source.ResolveDelta(remoteAnchor);
        }

        protected virtual SyncResult AcceptChanges(SyncDelta<TEntity, TVersion> remoteDelta)
        {
            return this.Target.AcceptChanges(remoteDelta);
        }
    }
}
