using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.Providers
{
    public class CachingChangeHistorySyncProviderV2<TEntity, TChangeHistory> : ChangeHistorySyncProvider<TEntity, TChangeHistory>
        where TEntity : class
        where TChangeHistory : IChangeHistory, new()
    {
        private LazyCache<IReadOnlyList<TChangeHistory>> FilteredChangeHistoryCache { get; set; }
        private LazyCache<SyncAnchor<TChangeHistory>> LastAnchorCache { get; set; }
        private Tuple<SyncAnchor<TChangeHistory>, SyncDelta<TEntity, TChangeHistory>> LastResolveDelta { get; set; }

        protected override IEnumerable<TChangeHistory> FilteredChangeHistory
        {
            get
            {
                return this.FilteredChangeHistoryCache.Value;
            }
        }

        public CachingChangeHistorySyncProviderV2(
            SyncReplicaInfo replicaInfo,
            ISyncRepository<Guid, TEntity> repository,
            ISyncRepository<int, TChangeHistory> changeHistory)
            : base(replicaInfo, repository, changeHistory)
        {
            // Anchor cache.
            this.FilteredChangeHistoryCache = new LazyCache<IReadOnlyList<TChangeHistory>>(base.FilteredChangeHistory.ToList);
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
            this.FilteredChangeHistoryCache.Invalidate();

            this.LastResolveDelta = null;
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

        public override SyncDelta<TEntity, TChangeHistory> ResolveDelta(SyncAnchor<TChangeHistory> remoteAnchor)
        {
            // See if we already have a delta.
            var lastResolvedDelta = this.LastResolveDelta;

            if (lastResolvedDelta != null &&
                this.AnchorsEqual(lastResolvedDelta.Item1, remoteAnchor))
            {
                return lastResolvedDelta.Item2;
            }

            var delta = base.ResolveDelta(remoteAnchor);

            this.LastResolveDelta = Tuple.Create(remoteAnchor, delta);

            return delta;
        }

        private bool AnchorsEqual(SyncAnchor<TChangeHistory> anchor1, SyncAnchor<TChangeHistory> anchor2)
        {
            if (anchor1.Count != anchor2.Count)
            {
                return false;
            }

            using (var enumerator1 = anchor1.GetEnumerator())
            using (var enumerator2 = anchor2.GetEnumerator())
            {
                while (enumerator1.MoveNext())
                {
                    if (!enumerator2.MoveNext())
                    {
                        throw new InvalidOperationException("Number of entities does not match.");
                    }

                    // Compare keys and values.
                    var kvp1 = enumerator1.Current;
                    var kvp2 = enumerator2.Current;

                    if (kvp1.Key != kvp2.Key ||
                        this.VersionComparer.Compare(kvp1.Value, kvp2.Value) != 0)
                    {
                        return false;
                    }
                }

                if (enumerator2.MoveNext())
                {
                    throw new InvalidOperationException("Number of entities does not match.");
                }
            }

            return true;
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