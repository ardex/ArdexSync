using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync
{
    /// <summary>
    /// Source of a sync operation.
    /// </summary>
    public interface ISyncSource<TEntity, TVersion> : ISyncAnchor<TVersion>
    {
        
    }
}
