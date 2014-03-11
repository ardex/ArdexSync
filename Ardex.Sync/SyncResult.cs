using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Ardex.Sync
{
    /// <summary>
    /// Contains information about a completed synchronisation.
    /// </summary>             
    [DataContract]
    public class SyncResult
    {
        /// <summary>
        /// Gets the objects inserted as part
        /// of this synchronisation operation.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public object[] Inserts { get; private set; }

        /// <summary>
        /// Gets the objects updated as part
        /// of this synchronisation operation.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public object[] Updates { get; private set; }

        /// <summary>
        /// Gets the objects deleted as part
        /// of this synchornisation operation.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public object[] Deletes { get; private set; }

        /// <summary>
        /// Sum of inserts and updates performed
        /// during this synchronisation operation.
        /// </summary>
        public int ChangeCount
        {
            get { return this.Inserts.Length + this.Updates.Length + this.Deletes.Length; }
        }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        public SyncResult() : this(null, null, null) { }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        public SyncResult(IEnumerable<object> inserts, IEnumerable<object> updates, IEnumerable<object> deletes)
        {
            this.Inserts = inserts == null ? new object[0] : inserts.ToArray();
            this.Updates = updates == null ? new object[0] : updates.ToArray();
            this.Deletes = deletes == null ? new object[0] : deletes.ToArray();
        }
    }

    /// <summary>
    /// Wraps results from multiple sync operations.
    /// </summary>
    [DataContract]
    public class MultiSyncResult : SyncResult
    {
        /// <summary>
        /// Results of each individual sync opreation.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public SyncResult[] SyncResults { get; private set; }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        public MultiSyncResult(params SyncResult[] syncResults) : base(
            syncResults.SelectMany(o => o.Inserts),
            syncResults.SelectMany(o => o.Updates),
            syncResults.SelectMany(o => o.Deletes))
        {
            if (syncResults == null) throw new ArgumentNullException("syncResults");

            this.SyncResults = syncResults;
        }
    }
}

