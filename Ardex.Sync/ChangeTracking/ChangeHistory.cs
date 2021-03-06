﻿using System;

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
        /// Backing field for Timestamp.
        /// </summary>
        private Timestamp _timestamp;

        /// <summary>
        /// Local ID (primary key).
        /// </summary>
        public int ChangeHistoryID { get; set; }

        /// <summary>
        /// Unique ID of the sync article that
        /// this change history entry relates to.
        /// </summary>
        public short ArticleID { get; set; }

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
        public string Timestamp
        {
            get
            {
                var t = _timestamp;

                return t == null ? null : t.ToString();
            }
            set
            {
                _timestamp = (value == null ? null : new Timestamp(value));
            }
        }

        /// <summary>
        /// Type of change.
        /// </summary>
        public string Action { get; set; }

        #region Tricky conversions

        Guid IChangeHistory.EntityGuid
        {
            get
            {
                if (string.IsNullOrEmpty(this.EntityGuid))
                {
                    return Guid.Empty;
                }

                return Guid.Parse(this.EntityGuid);
            }
            set
            {
                this.EntityGuid = (value == Guid.Empty ? null : value.ToString());
            }
        }

        Timestamp IChangeHistory.Timestamp
        {
            get
            {
                return _timestamp;
            }
            set
            {
                _timestamp = value;
            }
        }

        SyncEntityChangeAction IChangeHistory.Action
        {
            get
            {
                if (string.IsNullOrEmpty(this.Action))
                {
                    return SyncEntityChangeAction.None;
                }

                return (SyncEntityChangeAction)Enum.Parse(typeof(SyncEntityChangeAction), this.Action, true);
            }
            set
            {
                this.Action = (value == SyncEntityChangeAction.None ? null : value.ToString());
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
            proxy.ArticleID = other.ArticleID;
            proxy.ReplicaID = other.ReplicaID;
            proxy.Timestamp = other.Timestamp;
            proxy.EntityGuid = other.EntityGuid;
            proxy.Action = other.Action;
        }
    }
}
