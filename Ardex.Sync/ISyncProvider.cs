using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.Sync
{
    /// <summary>
    /// Complete sync provider, which includes both
    /// sync source and sync target functionality.
    /// </summary>
    public interface ISyncProvider<TEntity, TAnchor, TVersion> :
        ISyncSource<TEntity, TAnchor, TVersion>,
        ISyncTarget<TEntity, TAnchor, TVersion>
    {

    }
}
