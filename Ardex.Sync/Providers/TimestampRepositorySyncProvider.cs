using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync.Providers
{
    /// <summary>
    /// Timestamp-based sync provider which
    /// uses a repository for producing
    /// and accepting change delta.
    /// </summary>
    public class TimestampRepositorySyncProvider<TEntity> : ISyncProvider<IComparable, TEntity>
    {
        /// <summary>
        /// Unique identifier of this replica.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Gets the respository that this SyncOperation works with.
        /// </summary>
        public SyncRepository<TEntity> Repository { get; private set; }

        /// <summary>
        /// Provides means of uniquely identifying an entity.
        /// </summary>
        public UniqueIdMapping<TEntity> UniqueIdMapping { get; private set; }

        /// <summary>
        /// Provides means of getting a timestamp from an entity.
        /// </summary>
        public ComparableMapping<TEntity> TimestampMapping { get; private set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public TimestampRepositorySyncProvider(
            SyncID replicaID,
            SyncRepository<TEntity> repository,
            UniqueIdMapping<TEntity> uniqueIdMapping,
            ComparableMapping<TEntity> timestampMapping)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            if (uniqueIdMapping == null) throw new ArgumentNullException("uniqueIdMapping");
            if (timestampMapping == null) throw new ArgumentNullException("timestampMapping");

            this.ReplicaID = replicaID;
            this.Repository = repository;
            this.UniqueIdMapping = uniqueIdMapping;
            this.TimestampMapping = timestampMapping;
        }

        public Delta<IComparable, TEntity> ResolveDelta(IComparable lastSeenTimestamp, CancellationToken ct)
        {
            this.Repository.Lock.EnterReadLock();

            try
            {
                var anchor = this.LastAnchor();

                var changes = this.Repository
                    .Where(e => lastSeenTimestamp == null || this.TimestampMapping.Get(e).CompareTo(lastSeenTimestamp) > 0)
                    .OrderBy(e => this.TimestampMapping.Get(e))
                    .AsEnumerable();

                return new Delta<IComparable, TEntity>(anchor, changes);
            }
            finally
            {
                this.Repository.Lock.ExitReadLock();
            }
        }

        public SyncResult AcceptChanges(SyncID sourceReplicaID, Delta<IComparable, TEntity> delta, CancellationToken ct)
        {
            var repository = this.Repository;

            repository.Lock.EnterWriteLock();

            try
            {
                // From here on we have exclusive access to the
                // underlying collection and no reads or writes
                // can be performed until the lock is released.
                ct.ThrowIfCancellationRequested();

                var type = typeof(TEntity);
                var inserts = new List<object>();
                var updates = new List<object>();
                var deletes = new List<object>();
                var props = type.GetProperties();

                foreach (var change in delta.Changes.OrderBy(e => this.TimestampMapping.Get(e)))
                {
                    ct.ThrowIfCancellationRequested();

                    var changeUniqueID = this.UniqueIdMapping.Get(change);
                    var found = false;

                    foreach (var existingEntity in repository)
                    {
                        if (changeUniqueID == this.UniqueIdMapping.Get(existingEntity))
                        {
                            // Found.
                            var changeCount = 0;

                            foreach (var prop in props)
                            {
                                if (prop.CanRead && prop.CanWrite)
                                {
                                    var oldValue = prop.GetValue(existingEntity);
                                    var newValue = prop.GetValue(change);

                                    if (!object.Equals(oldValue, newValue))
                                    {
                                        prop.SetValue(existingEntity, newValue);
                                        changeCount++;
                                    }
                                }
                            }

                            if (changeCount != 0)
                            {
                                repository.Update(existingEntity);
                                updates.Add(existingEntity);
                            }

                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        repository.Insert(change);
                        inserts.Add(change);
                    }
                }

                ct.ThrowIfCancellationRequested();

                Debug.Print("{0} applied {1} {2} inserts originating at {3}.", this.ReplicaID, inserts.Count, type.Name, sourceReplicaID);
                Debug.Print("{0} applied {1} {2} updates originating at {3}.", this.ReplicaID, updates.Count, type.Name, sourceReplicaID);
                Debug.Print("{0} applied {1} {2} deletes originating at {3}.", this.ReplicaID, deletes.Count, type.Name, sourceReplicaID);

                var result = new SyncResult(inserts, updates, deletes);

                return result;
            }
            finally
            {
                repository.Lock.ExitWriteLock();
            }
        }

        public IComparable LastAnchor()
        {
            return this.Repository
                .Select(e => this.TimestampMapping.Get(e))
                .DefaultIfEmpty()
                .Max();
        }
    }
}
