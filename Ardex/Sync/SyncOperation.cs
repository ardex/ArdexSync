using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ardex.Sync
{
    public class SyncOperation<TAnchor, TChange> : SyncOperation
    {
        public ISyncSource<TAnchor, TChange> Source { get; private set; }
        public ISyncTarget<TAnchor, TChange> Target { get; private set; }
        public int BatchSize { get; set; }
        public SyncFilter<TChange> Filter { get; set; }

        public SyncOperation(ISyncSource<TAnchor, TChange> source, ISyncTarget<TAnchor, TChange> target)
        {
            this.Source = source;
            this.Target = target;

            // Defaults.
            this.BatchSize = 0;
        }

        protected override SyncResult SynchroniseDiff(CancellationToken ct)
        {
            var anchor = this.Target.LastAnchor();

            ct.ThrowIfCancellationRequested();

            var changes = this.Source.ResolveDelta(anchor, this.BatchSize, ct);

            ct.ThrowIfCancellationRequested();

            if (this.Filter != null)
            {
                changes = this.Filter(changes);
            }

            var result = this.ProcessChanges(changes, ct);

            return result;
        }

        protected virtual SyncResult ProcessChanges(IEnumerable<TChange> changes, CancellationToken ct)
        {
            var cleanup1 = this.Source as ISyncMetadataCleanup<TChange>;
            var cleanup2 = this.Target as ISyncMetadataCleanup<TChange>;

            if (cleanup1 != null || cleanup2 != null)
            {
                // Materialise changes since they will
                // need to be iterated over multiple times.
                changes = changes.ToArray();
                ct.ThrowIfCancellationRequested();
            }

            var result = this.Target.AcceptChanges(this.Source.ReplicaID, changes, ct);

            ct.ThrowIfCancellationRequested();
            if (cleanup1 != null) cleanup1.CleanUpSyncMetadata(changes);
            ct.ThrowIfCancellationRequested();
            if (cleanup2 != null) cleanup2.CleanUpSyncMetadata(changes);
            ct.ThrowIfCancellationRequested();

            return result;
        }
    }

    /// <summary>
    /// Base class for differential sync operations.
    /// </summary>
    public abstract class SyncOperation
    {
        #region Factory methods

        public static SyncOperation<TAnchor, TChange> Create<TAnchor, TChange>(
            ISyncSource<TAnchor, TChange> source, ISyncTarget<TAnchor, TChange> target)
        {
            return new SyncOperation<TAnchor, TChange>(source, target);
        }

        #endregion

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

        /// <summary>
        /// Creates a sync operation which encapsulates
        /// multiple chained sync operations.
        /// </summary>
        public static SyncOperation Chain(params SyncOperation[] syncOperations)
        {
            return new SyncOperationChain(syncOperations);
        }

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
            public SyncOperationChain(params SyncOperation[] syncOperations)
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
    }
}

