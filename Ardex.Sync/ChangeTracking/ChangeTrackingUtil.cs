using System;
using System.Linq;

using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync.ChangeTracking
{
    /// <summary>
    /// Simplifies working with change history repositories.
    /// </summary>
    internal static class ChangeTrackingUtil
    {
        /// <summary>
        /// Resolves the next available timestamp value
        /// for this node without taking out any locks.
        /// </summary>
        public static Timestamp ResolveNextTimestamp<T>(ISyncRepository<T> changeHistory, SyncID replicaID) where T : IChangeHistory
        {
            var timestamp = changeHistory
                .AsUnsafeEnumerable()
                .Where(ch => ch.ReplicaID == replicaID)
                .Select(ch => ch.Timestamp)
                .DefaultIfEmpty()
                .Max();

            return timestamp == null ? new Timestamp(1) : ++timestamp;
        }

        //public static void WriteChangeHistory<TEntity, TChangeHistory>(
        //    ISyncRepositoryWithChangeTracking<TEntity, TChangeHistory> repository,
        //    ISyncRepository<TChangeHistory> changeHistory,
        //    Func<TChangeHistory, TChangeHistory> changeHistoryFactory)
        //{
        //    if (repository.TrackInsert != null ||
        //        repository.TrackUpdate != null ||
        //        repository.TrackDelete != null)
        //    {
        //        throw new InvalidOperationException(
        //            "Unable to install change history link: repository already provisioned for change tracking.");
        //    }

        //    repository.TrackInsert = entity =>
        //    {
        //        changeHistory.ObtainExclusiveLock();

        //        try
        //        {
        //            var ch = changeHistoryFactory(entity);

        //            changeHistory.Insert(ch);
        //        }
        //        finally
        //        {
        //            changeHistory.ReleaseExclusiveLock();
        //        }
        //    };

        //    repository.TrackUpdate = entity =>
        //    {
        //        changeHistory.ObtainExclusiveLock();

        //        try
        //        {
        //            var ch = changeHistoryFactory(entity);

        //            changeHistory.Insert(ch);
        //        }
        //        finally
        //        {
        //            changeHistory.ReleaseExclusiveLock();
        //        }
        //    };

        //    // We are intentionally leaving out the delete.
        //    // It's up to the repository to detect that it's
        //    // not hooked up, and throw the exception.
        //}
    }
}
