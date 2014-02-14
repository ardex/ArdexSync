using System;
using System.Linq;

#if MONOTOUCH
using SQLite;
#endif

namespace Ardex.TestClient.Tests.Filtered.Entities
{
    public class ShortList
    {
        #if MONOTOUCH
        [PrimaryKey]
        #endif
        public int ShortListID { get; set; }
        public int SaleID { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public bool Expired { get; set; }

        // Added for replication.
        public int OwnerReplicaID { get; set; }
        public Guid EntityGuid { get; set; }
    }
}

