using System;

using Ardex.Sync;

namespace Ardex.TestClient
{
    public class DummyPermission : IEquatable<DummyPermission>
    {
        public Guid DummyPermissionID { get; set; }
        public int SourceReplicaID { get; set; }
        public int SourceDummyID { get; set; }
        public int DestinationReplicaID { get; set; }
        public bool Expired { get; set; }
        public Timestamp Timestamp { get; set; }

        public DummyPermission Clone()
        {
            return new DummyPermission
            {
                DummyPermissionID = this.DummyPermissionID,
                SourceReplicaID = this.SourceReplicaID,
                SourceDummyID = this.SourceDummyID,
                DestinationReplicaID = this.DestinationReplicaID,
                Expired = this.Expired,
                Timestamp = this.Timestamp
            };
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as DummyPermission);
        }

        public bool Equals(DummyPermission other)
        {
            return
                this.DummyPermissionID == other.DummyPermissionID &&
                this.SourceReplicaID == other.SourceReplicaID &&
                this.SourceDummyID == other.SourceDummyID &&
                this.DestinationReplicaID == other.DestinationReplicaID &&
                this.Expired == other.Expired &&
                this.Timestamp == other.Timestamp;
        }

        public override int GetHashCode()
        {
            return this.DummyPermissionID.GetHashCode();
        }
    }
}
