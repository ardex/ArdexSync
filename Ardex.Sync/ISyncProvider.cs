﻿using System;
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
    public interface ISyncProvider<TEntity, TVersion> :
        ISyncSource<TEntity, TVersion>,
        ISyncTarget<TEntity, TVersion>
    {

    }
}
