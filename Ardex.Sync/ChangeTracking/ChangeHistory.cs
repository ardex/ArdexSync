using System;

namespace Ardex.Sync.ChangeTracking
{
    /// <summary>
    /// Database-friendly implementation of IChangeHistory.
    /// Describes an entity change and provides
    /// metadata essential for sync operations.
    /// </summary>
    public class ChangeHistory : IChangeHistory
    {
        /// <summary>
        /// Local ID (primary key).
        /// </summary>
        public int ChangeHistoryID { get; set; }

        /// <summary>
        /// Unique identifier of the entity that was affected by the change.
        /// </summary>
        public string UniqueID { get; set; }

        /// <summary>
        /// ID of the replica (local or remote) which made the change.
        /// </summary>
        public string ReplicaID { get; set; }

        /// <summary>
        /// Replica-generated timestamp for the change.
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Type of change.
        /// </summary>
        public string Action { get; set; }

        #region Tricky conversions

        SyncID IChangeHistory.ReplicaID
        {
            get
            {
                return new SyncID(this.ReplicaID);
            }
            set
            {
                this.ReplicaID = value.ToString();
            }
        }

        SyncID IChangeHistory.UniqueID
        {
            get
            {
                return new SyncID(this.UniqueID);
            }
            set
            {
                this.UniqueID = value.ToString();
            }
        }

        Timestamp IChangeHistory.Timestamp
        {
            get
            {
                return new Timestamp(this.Timestamp);
            }
            set
            {
                this.Timestamp = value.ToString();
            }
        }

        ChangeHistoryAction IChangeHistory.Action
        {
            get
            {
                return (ChangeHistoryAction)Enum.Parse(typeof(ChangeHistoryAction), this.Action, true);
            }
            set
            {
                this.Action = value.ToString();
            }
        }

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ChangeHistory()
        {

        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public ChangeHistory(IChangeHistory other)
        {
            var proxy = (IChangeHistory)this;

            proxy.ChangeHistoryID = other.ChangeHistoryID;
            proxy.ReplicaID = other.ReplicaID;
            proxy.Timestamp = other.Timestamp;
            proxy.UniqueID = other.UniqueID;
            proxy.Action = other.Action;
        }
    }

    /// <summary>
    /// Defines supported change history action types.
    /// </summary>
    public enum ChangeHistoryAction
    {
        Insert,
        Update,
        Delete
    }
}
