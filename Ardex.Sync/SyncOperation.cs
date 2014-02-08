using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ardex.Sync
{
    /// <summary>
    /// Base class for differential sync operations.
    /// </summary>
    public abstract class SyncOperation
    {
        #region Static factory methods

        /// <summary>
        /// Creates a sync operation with the given source and target.
        /// </summary>
        public static BasicSyncOperation<TEntity, TAnchor, TVersion> Create<TEntity, TAnchor, TVersion>(
            ISyncSource<TEntity, TAnchor, TVersion> source, ISyncTarget<TEntity, TAnchor, TVersion> target)
        {
            return new BasicSyncOperation<TEntity, TAnchor, TVersion>(source, target);
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

