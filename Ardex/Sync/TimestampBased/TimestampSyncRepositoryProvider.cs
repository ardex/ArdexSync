using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.TimestampBased
{
    public class TimestampSyncRepositoryProvider<TEntity> : ISyncProvider<Timestamp, TEntity>
    {
        /// <summary>
        /// Unique identifier of this replica.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Gets the respository that this SyncOperation works with.
        /// </summary>
        public ISyncRepository<TEntity> Repository { get; private set; }

        /// <summary>
        /// Provides means of uniquely identifying an entity.
        /// </summary>
        public UniqueIdMapping<TEntity> UniqueIdMapping { get; private set; }

        /// <summary>
        /// Provides means of getting a timestamp from an entity.
        /// </summary>
        public TimestampMapping<TEntity> TimestampMapping { get; private set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public TimestampSyncRepositoryProvider(
            SyncID replicaID,
            ISyncRepository<TEntity> repository,
            UniqueIdMapping<TEntity> uniqueIdMapping,
            TimestampMapping<TEntity> timestampMapping)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            if (uniqueIdMapping == null) throw new ArgumentNullException("uniqueIdMapping");
            if (timestampMapping == null) throw new ArgumentNullException("timestampMapping");

            this.ReplicaID = replicaID;
            this.Repository = repository;
            this.UniqueIdMapping = uniqueIdMapping;
            this.TimestampMapping = timestampMapping;
        }

        public IEnumerable<TEntity> ResolveDelta(Timestamp lastSeenTimestamp, int batchSize, CancellationToken ct)
        {
            var changes = this.Repository
                .Where(e => lastSeenTimestamp == null || this.TimestampMapping.Get(e) > lastSeenTimestamp)
                .OrderBy(e => this.TimestampMapping.Get(e))
                .AsEnumerable();

            if (batchSize != 0)
            {
                changes = changes.Take(batchSize);
            }

            return changes;
        }

        public SyncResult AcceptChanges(SyncID sourceReplicaID, IEnumerable<TEntity> delta, CancellationToken ct)
        {
            this.Repository.ObtainExclusiveLock();

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

                foreach (var change in delta.OrderBy(e => this.TimestampMapping.Get(e)))
                {
                    ct.ThrowIfCancellationRequested();

                    var changeUniqueID = this.UniqueIdMapping.Get(change);
                    var found = false;

                    // Again, we are accessing snapshot in
                    // order to avoid recursive locking.
                    foreach (var existingEntity in this.Repository.AsUnsafeEnumerable())
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
                                this.Repository.DirectUpdate(existingEntity);
                                updates.Add(existingEntity);
                            }

                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        this.Repository.DirectInsert(change);
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
                this.Repository.ReleaseExclusiveLock();
            }
        }

        public Timestamp LastAnchor()
        {
            return this.Repository
                .Select(e => this.TimestampMapping.Get(e))
                .DefaultIfEmpty()
                .Max();
        }
    }
}
