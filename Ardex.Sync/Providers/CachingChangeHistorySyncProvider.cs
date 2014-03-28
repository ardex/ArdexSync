using System;
using System.Diagnostics;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.Providers
{
    public class CachingChangeHistorySyncProvider<TEntity, TChangeHistory> : ChangeHistorySyncProvider<TEntity, TChangeHistory>
        where TEntity : class
        where TChangeHistory : IChangeHistory, new()
    {
        private LazyCache<SyncAnchor<TChangeHistory>> LastAnchorCache { get; set; }

        public CachingChangeHistorySyncProvider(
            SyncReplicaInfo replicaInfo,
            ISyncRepository<Guid, TEntity> repository,
            ISyncRepository<int, TChangeHistory> changeHistory) 
            : base(replicaInfo, repository, changeHistory)
        {
            // Anchor cache.
            this.LastAnchorCache = new LazyCache<SyncAnchor<TChangeHistory>>(base.LastAnchor);

            this.Repository.EntityInserted += this.InvalidateCache;
            this.Repository.EntityUpdated += this.InvalidateCache;
            this.Repository.EntityDeleted += this.InvalidateCache;
            this.ChangeHistory.EntityInserted += this.InvalidateCache;
            this.ChangeHistory.EntityUpdated += this.InvalidateCache;
            this.ChangeHistory.EntityDeleted += this.InvalidateCache;
        }

        private void InvalidateCache<T>(T _)
        {
            this.LastAnchorCache.Invalidate();
        }

        public override SyncAnchor<TChangeHistory> LastAnchor()
        {
            var sw = Stopwatch.StartNew();

            try
            {
                using (this.ChangeHistory.SyncLock.ReadLock())
                {
                    return this.LastAnchorCache.Value;
                }
            }
            finally
            {
                Debug.WriteLine("{0}.LastAnchor() completed in {1:0.###} seconds.", this.GetType().Name, (float)sw.ElapsedMilliseconds / 1000);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unhook events to help the GC do its job.
                this.Repository.EntityInserted -= this.InvalidateCache;
                this.Repository.EntityUpdated -= this.InvalidateCache;
                this.Repository.EntityDeleted -= this.InvalidateCache;
                this.ChangeHistory.EntityInserted -= this.InvalidateCache;
                this.ChangeHistory.EntityUpdated -= this.InvalidateCache;
                this.ChangeHistory.EntityDeleted -= this.InvalidateCache;

                // Release refs.
                this.LastAnchorCache = null;
            }

            base.Dispose(disposing);
        }
    }
}