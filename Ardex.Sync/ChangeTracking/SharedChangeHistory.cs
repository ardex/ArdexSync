using System;

namespace Ardex.Sync.ChangeTracking 
{
    /// <summary>
    /// Change history entry which tracks
    /// changes in multiple articles.
    /// </summary>
    public class SharedChangeHistory : ISharedChangeHistory
    {
        /// <summary>
        /// Local ID (primary key).
        /// </summary>
        public int ChangeHistoryID { get; set; }

        /// <summary>
        /// Unique identifier of the entity that was affected by the change.
        /// </summary>
        public string EntityGuid { get; set; }

        /// <summary>
        /// ID of the replica (local or remote) which made the change.
        /// </summary>
        public int ReplicaID { get; set; }

        /// <summary>
        /// Replica-generated timestamp for the change.
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Type of change.
        /// </summary>
        public string Action { get; set; }

        #region Tricky conversions

        Guid IChangeHistory.EntityGuid
        {
            get
            {
                return Guid.Parse(this.EntityGuid);
            }
            set
            {
                this.EntityGuid = value.ToString();
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

        SyncEntityChangeAction IChangeHistory.Action
        {
            get
            {
                return (SyncEntityChangeAction)Enum.Parse(typeof(SyncEntityChangeAction), this.Action, true);
            }
            set
            {
                this.Action = value.ToString();
            }
        }

        #endregion

        /// <summary>
        /// Unique ID of the sync article that
        /// this change history entry relates to.
        /// </summary>
        public short ArticleID { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SharedChangeHistory()
        {

        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public SharedChangeHistory(ISharedChangeHistory other)
        {
            var proxy = (ISharedChangeHistory)this;

            proxy.ChangeHistoryID = other.ChangeHistoryID;
            proxy.ReplicaID = other.ReplicaID;
            proxy.Timestamp = other.Timestamp;
            proxy.EntityGuid = other.EntityGuid;
            proxy.Action = other.Action;
            proxy.ArticleID = other.ArticleID;
        }

        public override string ToString()
        {
            return Reflect.ToString(this);
        }
    }
}