using System;

namespace Ardex.Sync.EntityMapping
{
    /// <summary>
    /// Provides mapping for the replica ID which owns this record.
    /// </summary>
    public delegate int SyncEntityOwnerMapping<TEntity>(TEntity entity);
}