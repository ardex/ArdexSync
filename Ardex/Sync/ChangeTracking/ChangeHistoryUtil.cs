using System;
using System.Linq;

using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.ChangeTracking
{
    /// <summary>
    /// Simplifies working with change history repositories.
    /// </summary>
    internal static class ChangeHistoryUtil
    {
        /// <summary>
        /// Resolves the next available timestamp value
        /// for this node without taking out any locks.
        /// </summary>
        public static Timestamp ResolveNextTimestamp(ISyncRepository<IChangeHistory> changeHistory, SyncID replicaID)
        {
            var timestamp = changeHistory
                .AsUnsafeEnumerable()
                .Where(ch => ch.ReplicaID == replicaID)
                .Select(ch => ch.Timestamp)
                .DefaultIfEmpty()
                .Max();

            return timestamp == null ? new Timestamp(1) : ++timestamp;
        }

        /// <summary>
        /// Creates the necessary ChangeHistory entries.
        /// </summary>
        public static void WriteLocalChangeHistory<TEntity>(
            ISyncRepository<IChangeHistory> changeHistory,
            TEntity entity,
            SyncID replicaID,
            UniqueIdMapping<TEntity> uniqueIdMapping,
            ChangeHistoryAction action)
        {
            changeHistory.ObtainExclusiveLock();

            try
            {
                var timestamp = ChangeHistoryUtil.ResolveNextTimestamp(changeHistory, replicaID);

                ChangeHistoryUtil.WriteChangeHistoryUnsafe(changeHistory, entity, replicaID, timestamp, uniqueIdMapping, action);
            }
            finally
            {
                changeHistory.ReleaseExclusiveLock();
            }
        }

        /// <summary>
        /// Creates the necessary ChangeHistory entries.
        /// </summary>
        public static void WriteRemoteChangeHistory<TEntity>(
            ISyncRepository<IChangeHistory> changeHistory,
            TEntity entity,
            SyncID replicaID,
            Timestamp timestamp,
            UniqueIdMapping<TEntity> uniqueIdMapping,
            ChangeHistoryAction action)
        {
            if (timestamp == null) throw new ArgumentNullException("timestamp");

            changeHistory.ObtainExclusiveLock();

            try
            {
                ChangeHistoryUtil.WriteChangeHistoryUnsafe(changeHistory, entity, replicaID, timestamp, uniqueIdMapping, action);
            }
            finally
            {
                changeHistory.ReleaseExclusiveLock();
            }
        }

        /// <summary>
        /// Creates the necessary ChangeHistory entries without taking out any locks.
        /// </summary>
        private static void WriteChangeHistoryUnsafe<TEntity>(
            ISyncRepository<IChangeHistory> changeHistory,
            TEntity entity,
            SyncID replicaID,
            Timestamp timestamp,
            UniqueIdMapping<TEntity> uniqueIdMapping,
            ChangeHistoryAction action)
        {
            var ch = (IChangeHistory)new ChangeHistory();

            // Auto-generate PK.
            ch.ChangeHistoryID = changeHistory
                .AsUnsafeEnumerable()
                .Select(c => c.ChangeHistoryID)
                .DefaultIfEmpty()
                .Max() + 1;

            ch.ReplicaID = replicaID;
            ch.UniqueID = uniqueIdMapping.Get(entity);
            ch.Timestamp = timestamp;
            ch.Action = action;

            changeHistory.DirectInsert(ch);
        }
    }
}
