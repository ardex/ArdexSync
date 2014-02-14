using System;

namespace Ardex.TestClient.Tests.Filtered.Entities
{
    public class ShortListItem
    {
        public int ShortListItemID { get; set; }
        public int ShortListID { get; set; }
        public int HorseID { get; set; }
        public bool Expired { get; set; }

        // Added for replication.
        public int OwnerReplicaID { get; set; }
        public Guid EntityGuid { get; set; }
    }
}

