using System;
using System.Collections.Generic;

using Ardex.Sync.EntityMapping;

namespace Ardex.Sync
{
    /// <summary>
    /// Dictionary where the key is the replica ID,
    /// and the value is the maximum known version value
    /// for any entity associated with this replica ID.
    /// </summary>
    public class SyncAnchor<TVersion> : Dictionary<int, TVersion>
    {
        
    }
}