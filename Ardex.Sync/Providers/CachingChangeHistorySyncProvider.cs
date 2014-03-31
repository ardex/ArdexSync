//#define CACHE_ANCHOR
//#define CACHE_DELTA

using System;

#if CACHE_ANCHOR
using System.Diagnostics;
#endif

#if CACHE_DELTA
using System.Threading;
#endif

using Ardex.Sync.ChangeTracking;
using Ardex.Threading;

namespace Ardex.Sync.Providers
{
    public class CachingChangeHistorySyncProvider<TEntity, TChangeHistory> : ChangeHistorySyncProvider<TEntity, TChangeHistory>
        where TEntity : class
        where TChangeHistory : IChangeHistory, new()
    {
        #if CACHE_ANCHOR
        private LazyCache<SyncAnchor<TChangeHistory>> LastAnchorCache { get; set; }
        #endif

        #if CACHE_DELTA
        private Tuple<SyncAnchor<TChangeHistory>, SyncDelta<TEntity, TChangeHistory>> LastResolveDelta;
        #endif

        // Sequential value resolution optimisations.
        private int LastChangeHistoryID = -1;
        private Timestamp LastTimestamp;

        public CachingChangeHistorySyncProvider(
            SyncReplicaInfo replicaInfo,
            ISyncRepository<Guid, TEntity> repository,
            ISyncRepository<int, TChangeHistory> changeHistory)
            : base(replicaInfo, repository, changeHistory)
        {
            #if CACHE_ANCHOR
            this.LastAnchorCache = new LazyCache<SyncAnchor<TChangeHistory>>(base.LastAnchor);
            #endif

            this.Repository.TrackedChange += this.EntityChanged;
            this.ChangeHistory.TrackedChange += this.ChangeHistoryChanged;
            this.ChangeHistory.UntrackedChange += this.ChangeHistoryChanged;
        }

        private void EntityChanged(TEntity entity, SyncEntityChangeAction changeAction)
        {
            #if CACHE_ANCHOR
            this.LastAnchorCache.Invalidate();
            #endif

            #if CACHE_DELTA
            Interlocked.Exchange(ref this.LastResolveDelta, null);
            #endif
        }

        private void ChangeHistoryChanged(TChangeHistory changeHistory, SyncEntityChangeAction changeAction)
        {
            if (changeAction == SyncEntityChangeAction.Insert)
            {
                // Choose greater value.
                Atomic.Transform(
                    ref this.LastChangeHistoryID,
                    changeHistory.ChangeHistoryID,
                    Math.Max
                );

                Atomic.Transform(
                    ref this.LastTimestamp,
                    changeHistory.Timestamp,
                    (old, nu) => old == null || nu.CompareTo(old) > 0 ? nu : old
                );
            }

            #if CACHE_ANCHOR
            this.LastAnchorCache.Invalidate();
            #endif
        }

        #if CACHE_ANCHOR

        public override SyncAnchor<TChangeHistory> LastAnchor()
        {
            var sw = Stopwatch.StartNew();

            try
            {
                return this.LastAnchorCache.Value;
            }
            finally
            {
                Debug.WriteLine(
                    "{0}.LastAnchor() completed in {1:0.###} seconds.",
                    string.Format("{0}<{1}, {2}>", this.GetType().Name.Replace("`2", ""), typeof(TEntity).Name, typeof(TChangeHistory).Name),
                    (float)sw.ElapsedMilliseconds / 1000);
            }
        }

        #endif

        #if CACHE_DELTA

        public override SyncDelta<TEntity, TChangeHistory> ResolveDelta(SyncAnchor<TChangeHistory> remoteAnchor)
        {
            var result = Atomic.Transform(ref this.LastResolveDelta, lastResolvedDelta =>
            {
                if (lastResolvedDelta != null &&
                    this.AnchorsEqual(lastResolvedDelta.Item1, remoteAnchor))
                {
                    return lastResolvedDelta;
                }

                var delta = base.ResolveDelta(remoteAnchor);

                return Tuple.Create(remoteAnchor, delta);
            });

            return result.Item2;
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

        #endif

        // ChangeHistoryID resolution optimisation.
        protected override int NextChangeHistoryID()
        {
            var nextChangeHistoryIdDel = (Func<int>)base.NextChangeHistoryID;

            return Atomic.Transform(
                ref this.LastChangeHistoryID,
                nextChangeHistoryIdDel,
                (lastChangeHistoryID, valueFactory) => lastChangeHistoryID == -1 ? valueFactory() : lastChangeHistoryID + 1
            );
        }

        // Timestamp resolution optimisation.
        protected override Timestamp NextTimestamp()
        {
            var nextTimestampDel = (Func<Timestamp>)base.NextTimestamp;

            return Atomic.Transform(
                ref this.LastTimestamp,
                nextTimestampDel,
                (lastTimestamp, valueFactory) => lastTimestamp == null ? valueFactory() : ++lastTimestamp
            );
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unhook events to help the GC do its job.
                this.Repository.TrackedChange -= this.EntityChanged;
                this.ChangeHistory.TrackedChange -= this.ChangeHistoryChanged;
                this.ChangeHistory.UntrackedChange -= this.ChangeHistoryChanged;

                // Release refs.
                #if CACHE_ANCHOR
                this.LastAnchorCache = null;
                #endif
            }

            base.Dispose(disposing);
        }
    }
}