using System;

namespace Ardex.Sync.ChangeTracking
{
    /// <summary>
    /// Describes an entity change and provides
    /// metadata essential for sync operations.
    /// </summary>
    public interface IChangeHistory
    {
        /// <summary>
        /// Local ID (primary key).
        /// </summary>
        int ChangeHistoryID { get; set; }

        /// <summary>
        /// ID of the node (local or remote) which made the change.
        /// </summary>
        SyncID ReplicaID { get; set; }

        /// <summary>
        /// Node-generated timestamp for the change.
        /// </summary>
        Timestamp Timestamp { get; set; }

        /// <summary>
        /// Unique identifier of the entity that was affected by the change.
        /// </summary>
        string UniqueID { get; set; }

        /// <summary>
        /// Type of change.
        /// </summary>
        ChangeHistoryAction Action { get; set; }
    }
}
