using System;

#if MONOTOUCH
using SQLite;
#endif

namespace Ardex.TestClient.Tests.Filtered.Entities
{
    public class ShortListItem
    {
        #if MONOTOUCH
        [PrimaryKey]
        #endif
        public int ShortListItemID { get; set; }
        public int ShortListID { get; set; }
        public int HorseID { get; set; }
        public bool Expired { get; set; }

        // Added for replication.
        public Guid EntityGuid { get; set; }
    }
}

