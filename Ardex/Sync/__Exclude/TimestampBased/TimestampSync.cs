using System.Threading;

namespace Ardex.Sync.TimestampBased
{
    public class TimestampSync<TEntity> : SyncOperation where TEntity : class
    {
        public ITimestampSyncSource<TEntity> Source { get; private set; }
        public ITimestampSyncTarget<TEntity> Target { get; private set; }

        /// <summary>
        /// Optional change filter.
        /// </summary>
        public SyncFilter<TEntity> Filter { get; set; }

        /// <summary>
        /// Maximum change batch size (default = 0, unlimited).
        /// </summary>
        public int BatchSize { get; set; }

        public TimestampSync(ITimestampSyncSource<TEntity> source, ITimestampSyncTarget<TEntity> target)
        {
            this.Source = source;
            this.Target = target;

            // Defaults.
            this.BatchSize = 0; // Unlimited.
        }

        protected override SyncResult SynchroniseDiff(CancellationToken ct)
        {
            var lastSeenTimestamp = this.Target.LastAnchor();
            
            ct.ThrowIfCancellationRequested();

            var changes = this.Source.ResolveDelta(lastSeenTimestamp, this.BatchSize, ct);

            ct.ThrowIfCancellationRequested();

            // Apply filter if it is a filtered sync operation.
            if (this.Filter != null)
            {
                changes = this.Filter(changes);
            }

            var result = this.Target.AcceptChanges(this.Source.ReplicaID, changes, ct);

            return result;
        }
    } 

    ///// <summary>
    ///// Provides download-only sync functionality for a repository.
    ///// </summary>
    //public class TimestampSyncz<TEntity> : SyncOperation where TEntity : class
    //{
    //    /// <summary>
    //    /// Gets the respository that this SyncOperation works with.
    //    /// </summary>
    //    public ISyncRepository<TEntity> Repository { get; private set; }

    //    /// <summary>
    //    /// Provides means of uniquely identifying an entity.
    //    /// </summary>
    //    public UniqueIdMapping<TEntity> UniqueIdMapping { get; private set; }

    //    /// <summary>
    //    /// Provides means of getting a timestamp from an entity.
    //    /// </summary>
    //    public TimestampMapping<TEntity> TimestampMapping { get; private set; }

    //    /// <summary>
    //    /// Produces entities for diff sync after the given timestamp.
    //    /// </summary>
    //    private readonly Func<Timestamp, IEnumerable<TEntity>> GetChanges;

    //    /// <summary>
    //    /// Optional change filter.
    //    /// </summary>
    //    public SyncFilter<TEntity> Filter { get; set; }

    //    /// <summary>
    //    /// Creates a new instance of the class.
    //    /// </summary>
    //    public TimestampSync(
    //        ISyncRepository<TEntity> repository,
    //        UniqueIdMapping<TEntity> uniqueIdMapping,
    //        TimestampMapping<TEntity> timestampMapping,
    //        Func<Timestamp, IEnumerable<TEntity>> getChanges)
    //    {
    //        if (repository == null) throw new ArgumentNullException("repository");
    //        if (uniqueIdMapping == null) throw new ArgumentNullException("uniqueIdMapping");
    //        if (timestampMapping == null) throw new ArgumentNullException("timestampMapping");
    //        if (getChanges == null) throw new ArgumentNullException("getChanges");

    //        this.Repository = repository;
    //        this.UniqueIdMapping = uniqueIdMapping;
    //        this.TimestampMapping = timestampMapping;
    //        this.GetChanges = getChanges;
    //    }

    //    /// <summary>
    //    /// Differential synchronisation implementation.
    //    /// </summary>
    //    protected override SyncResult SynchroniseDiff(CancellationToken ct)
    //    {
    //        // Initially we are relying on
    //        // locking implemented by the repo.
    //        var maxTimestamp = this.MaxTimestamp(this.Repository.AsEnumerable(), this.TimestampMapping.Get);

    //        ct.ThrowIfCancellationRequested();

    //        var changes = this.ResolveChanges(maxTimestamp);

    //        ct.ThrowIfCancellationRequested();

    //        if (changes.Length == 0)
    //        {
    //            // No point in taking out the write lock.
    //            return new SyncResult();
    //        }

    //        var result = this.ApplyChanges(this.Repository, changes, ct);

    //        return result;
    //    }

    //    private TEntity[] ResolveChanges(Timestamp maxRowTimestamp)
    //    {
    //        var changes = this.GetChanges(maxRowTimestamp);

    //        if (this.Filter != null)
    //        {
    //            changes = this.Filter(changes);
    //        }

    //        return changes.ToArray();
    //    }

    //    private SyncResult ApplyChanges(ISyncRepository<TEntity> repository, TEntity[] changes, CancellationToken ct)
    //    {
    //        repository.ObtainExclusiveLock();

    //        try
    //        {
    //            // From here on we have exclusive access to the
    //            // underlying collection and no reads or writes
    //            // can be performed until the lock is released.
    //            ct.ThrowIfCancellationRequested();

    //            var type = typeof(TEntity);
    //            var inserts = new List<object>();
    //            var updates = new List<object>();
    //            var deletes = new List<object>();
    //            var props = type.GetProperties();

    //            foreach (var change in changes.OrderBy(e => this.TimestampMapping.Get(e)))
    //            {
    //                ct.ThrowIfCancellationRequested();

    //                var changeUniqueID = this.UniqueIdMapping.Get(change);
    //                var found = false;

    //                // Again, we are accessing snapshot in
    //                // order to avoid recursive locking.
    //                foreach (var existingEntity in repository.AsUnsafeEnumerable())
    //                {
    //                    if (changeUniqueID == this.UniqueIdMapping.Get(existingEntity))
    //                    {
    //                        // Found.
    //                        var changeCount = 0;

    //                        foreach (var prop in props)
    //                        {
    //                            if (prop.CanRead && prop.CanWrite)
    //                            {
    //                                var oldValue = prop.GetValue(existingEntity);
    //                                var newValue = prop.GetValue(change);

    //                                #if FORCE_FULL_SYNC
    //                                const bool force = true;
    //                                #else
    //                                const bool force = false;
    //                                #endif

    //                                if (force || !object.Equals(oldValue, newValue))
    //                                {
    //                                    prop.SetValue(existingEntity, newValue);
    //                                    changeCount++;
    //                                }
    //                            }
    //                        }

    //                        if (changeCount != 0)
    //                        {
    //                            repository.DirectUpdate(existingEntity);
    //                            updates.Add(existingEntity);
    //                        }

    //                        found = true;
    //                        break;
    //                    }
    //                }

    //                if (!found)
    //                {
    //                    repository.DirectInsert(change);
    //                    inserts.Add(change);
    //                }
    //            }

    //            ct.ThrowIfCancellationRequested();

    //            Debug.Print("{0} inserts applied: {1}.", type.Name, inserts.Count);
    //            Debug.Print("{0} updates applied: {1}.", type.Name, updates.Count);
    //            Debug.Print("{0} deletes applied: {1}.", type.Name, deletes.Count);

    //            var result = new SyncResult(inserts, updates, deletes);

    //            return result;
    //        }
    //        finally
    //        {
    //            repository.ReleaseExclusiveLock();
    //        }
    //    }

    //    /// <summary>
    //    /// Returns the greatest RowTimestamp value.
    //    /// Does not take any synchronisation locks.
    //    /// </summary>
    //    protected Timestamp MaxTimestamp(IEnumerable<TEntity> entities, Func<TEntity, Timestamp> getTimestampFunc)
    //    {
    //        var maxTimestampEntity = entities
    //            .OrderByDescending(e => getTimestampFunc(e))
    //            .FirstOrDefault();

    //        return maxTimestampEntity == null ? null : getTimestampFunc(maxTimestampEntity);
    //    }
    //}
}

