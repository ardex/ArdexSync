using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ardex.Sync
{
    /// <summary>
    /// Basic sync operation implementation.
    /// </summary>
    public class SyncOperation<TAnchor, TChange> : SyncOperation
    {
        /// <summary>
        /// Sync operation source or provider.
        /// Resolves the change delta in a differential sync operation
        /// based on anchor info provided by the sync target.
        /// </summary>
        public ISyncSource<TAnchor, TChange> Source { get; private set; }

        /// <summary>
        /// Sync operation target or provider.
        /// Produces anchor info for the
        /// source and accepts changes.
        /// </summary>
        public ISyncTarget<TAnchor, TChange> Target { get; private set; }

        ///// <summary>
        ///// Allows limiting the maximum number of 
        ///// changes returned by the sync source.
        ///// The default value is 0 (unlimited).
        ///// </summary>
        //public int BatchSize { get; set; }

        /// <summary>
        /// Optional filter applied to the change
        /// delta returned by the source. Filters
        /// and/or transforms the changes before
        /// they are accepted by the target.
        /// </summary>
        public SyncFilter<TChange> Filter { get; set; }

        /// <summary>
        /// Creates a new SyncOperation instance.
        /// Consider using SyncOperation.Create for convenience.
        /// </summary>
        public SyncOperation(ISyncSource<TAnchor, TChange> source, ISyncTarget<TAnchor, TChange> target)
        {
            this.Source = source;
            this.Target = target;

            // Defaults.
            //this.BatchSize = 0; // Unlimited.
        }

        /// <summary>
        /// Differential synchronisation implementation.
        /// </summary>
        protected override SyncResult SynchroniseDiff(CancellationToken ct)
        {
            // Determine 
            var anchor = this.Target.LastAnchor();
            ct.ThrowIfCancellationRequested();

            var delta = this.Source.ResolveDelta(anchor, /*this.BatchSize,*/ ct);
            ct.ThrowIfCancellationRequested();

            if (this.Filter != null)
            {
                delta = this.Filter(delta);
                ct.ThrowIfCancellationRequested();
            }

            // See if either source or target support metadata cleanup.
            var sourceCleanup = this.Source as ISyncMetadataCleanup<TChange>;
            var targetCleanup = this.Target as ISyncMetadataCleanup<TChange>;

            if (sourceCleanup != null || targetCleanup != null)
            {
                // Materialise changes since they will
                // need to be iterated over multiple times.
                delta = delta.ToArray();
                ct.ThrowIfCancellationRequested();
            }

            // Accept changes.
            var result = this.Target.AcceptChanges(this.Source.ReplicaID, delta, ct);
            ct.ThrowIfCancellationRequested();

            // Source sync metdatata cleanup.
            if (sourceCleanup != null) sourceCleanup.CleanUpSyncMetadata(delta);
            ct.ThrowIfCancellationRequested();

            // Target sync metdatata cleanup.
            if (targetCleanup != null) targetCleanup.CleanUpSyncMetadata(delta);
            ct.ThrowIfCancellationRequested();

            return result;
        }
    }

    /// <summary>
    /// Base class for differential sync operations.
    /// </summary>
    public abstract class SyncOperation
    {
        #region Static factory methods

        /// <summary>
        /// Creates a sync operation with the given source and target.
        /// </summary>
        public static SyncOperation<TAnchor, TChange> Create<TAnchor, TChange>(
            ISyncSource<TAnchor, TChange> source, ISyncTarget<TAnchor, TChange> target)
        {
            return new SyncOperation<TAnchor, TChange>(source, target);
        }

        /// <summary>
        /// Creates a sync operation which encapsulates
        /// multiple chained sync operations.
        /// </summary>
        public static SyncOperation Chain(params SyncOperation[] syncOperations)
        {
            return new SyncOperationChain(syncOperations);
        }

        #endregion

        #region Fields and properties

        /// <summary>
        /// Lock used to ensure that only one sync operation runs at any given time.
        /// </summary>
        private readonly SemaphoreSlim SyncLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Backing field for SyncTasksInProgress.
        /// </summary>
        private readonly List<Task<SyncResult>> __syncTasksInProgress = new List<Task<SyncResult>>();

        /// <summary>
        /// Returns the currently running sync tasks.
        /// </summary>
        public Task<SyncResult>[] SyncTasksInProgress
        {
            get
            {
                lock (__syncTasksInProgress)
                {
                    return __syncTasksInProgress.ToArray();
                }
            }
        }

        #endregion

        #region Sync implementation

        /// <summary>
        /// Kicks off the asynchronous synchronisation.
        /// </summary>
        public Task<SyncResult> SynchroniseDiffAsync()
        {
            return this.SynchroniseDiffAsync(CancellationToken.None);
        }

        /// <summary>
        /// Kicks off the asynchronous synchronisation.
        /// </summary>
        public async Task<SyncResult> SynchroniseDiffAsync(CancellationToken ct)
        {
            // Wire up the task.
            var syncTask = Task.Run(async () =>
            {
                // We want to wait for the semaphore inside the
                // task - it's a legitimate part of the process.
                await this.SyncLock.WaitAsync(ct).ConfigureAwait(false);

                ct.ThrowIfCancellationRequested();

                try
                {
                    return this.SynchroniseDiff(ct);
                }
                finally
                {
                    this.SyncLock.Release();
                }
            }, ct);

            // Update the list of pending tasks.
            lock (__syncTasksInProgress)
            {
                __syncTasksInProgress.Add(syncTask);
            }

            try
            {
                // Await the task and return result.
                return await syncTask.ConfigureAwait(false);
            }
            finally
            {
                lock (__syncTasksInProgress)
                {
                    __syncTasksInProgress.Remove(syncTask);
                }
            }
        }

        /// <summary>
        /// When overridden in a derived class,
        /// performs differential synchronisation.
        /// </summary>
        protected abstract SyncResult SynchroniseDiff(CancellationToken ct);

        #endregion

        #region Private types

        /// <summary>
        /// Represents a sync operation which encapsulates
        /// multiple chained sync operations.
        /// </summary>
        private class SyncOperationChain : SyncOperation
        {
            /// <summary>
            /// The chained operations.
            /// </summary>
            public SyncOperation[] SyncOperations { get; private set; }

            /// <summary>
            /// Creates a new instance of the class.
            /// </summary>
            public SyncOperationChain(SyncOperation[] syncOperations)
            {
                this.SyncOperations = syncOperations;
            }

            /// <summary>
            /// Calls SynchroniseDiff on each sync operation
            /// in the chain and returns the combined result.
            /// </summary>
            protected override SyncResult SynchroniseDiff(CancellationToken ct)
            {
                var results = new List<SyncResult>();

                foreach (var syncOperation in this.SyncOperations)
                {
                    var result = syncOperation.SynchroniseDiff(ct);

                    results.Add(result);
                }

                return new MultiSyncResult(results.ToArray());
            }
        }

        #endregion
    }
}

