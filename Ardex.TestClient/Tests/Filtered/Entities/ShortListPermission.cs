using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.TestClient.Tests.Filtered.Entities
{
    public class ShortListPermission
    {
        public int PermissionID { get; set; }
        public int ShortListID { get; set; }
        public bool Expired { get; set; }
        public int GranteeReplicaID { get; set; }
        public int GrantorReplicaID { get; set; }
        public Guid EntityGuid { get; set; }
    }
}
