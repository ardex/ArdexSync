using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.Sync.ChangeTracking
{
    public interface ISharedChangeHistory : IChangeHistory
    {
        /// <summary>
        /// Unique ID of the change article that
        /// this change history entry relates to.
        /// </summary>
        SyncID ArticleID { get; set; }
    }
}