using System.Runtime.Serialization;

namespace Ardex.Sync
{
    /// <summary>
    /// Represents an individual sync anchor entry.
    /// </summary>
    [DataContract]
    public class SyncAnchorEntry<TVersion>
    {
        /// <summary>
        /// Unique ID of the data replica.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int ReplicaID { get; private set; }

        /// <summary>
        /// Last known version entry for the given replica.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TVersion MaxVersion { get; private set; }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public SyncAnchorEntry(int syncReplicaID, TVersion version)
        {
            this.ReplicaID = syncReplicaID;
            this.MaxVersion = version;
        }
    }
}