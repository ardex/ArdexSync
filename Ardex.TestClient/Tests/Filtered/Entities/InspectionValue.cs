using System;

namespace Ardex.TestClient.Tests.Filtered.Entities
{
    public class InspectionValue
    {
        public int ValueID { get; set; }
        public int CriteriaID { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public bool Expired { get; set; }

        // Added for replication.
        public int OwnerReplicaID { get; set; }
        public Guid EntityGuid { get; set; }
    }
}

