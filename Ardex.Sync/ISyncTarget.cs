using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync
{
    /// <summary>
    /// Target of a sync operation.
    /// </summary>
    public interface ISyncTarget<TEntity, TVersion> : ISyncAnchor<TVersion>
    {
        
    }
}
