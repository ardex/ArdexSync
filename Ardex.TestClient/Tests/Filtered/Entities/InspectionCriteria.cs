using System;

#if MONOTOUCH
using SQLite;
#endif

namespace Ardex.TestClient.Tests.Filtered.Entities
{
    public class InspectionCriteria
    {
        #if MONOTOUCH
        [PrimaryKey]
        #endif
        public int CriteriaID { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public bool Expired { get; set; }

        // Added for replication.
        public Guid EntityGuid { get; set; }
    }
}

