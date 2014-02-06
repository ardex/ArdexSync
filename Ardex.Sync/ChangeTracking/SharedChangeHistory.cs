using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.Sync.ChangeTracking 
{
    public class SharedChangeHistory : ChangeHistory, ISharedChangeHistory
    {
        public SyncID ArticleID { get; set; }
    }
}