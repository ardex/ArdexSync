using System;
using System.Runtime.Serialization;

namespace Ardex.Sync
{
    /// <summary>
    /// Describes a data replica.
    /// </summary>
    [DataContract]
    public class SyncReplicaInfo
    {
        /// <summary>
        /// Unique replica ID.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int ReplicaID { get; private set; }

        /// <summary>
        /// Friendly replica name.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Name { get; private set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public SyncReplicaInfo(int replicaID, string name)
        {
            this.ReplicaID = replicaID;
            this.Name = name;
        }

        /// <summary>
        /// Returns a human-readable replica description.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                "{0} (id = {1} / 0x{2})",
                this.Name,
                this.ReplicaID,
                Convert.ToString(this.ReplicaID, 16)
            );
        }
    }
}
