using System;

namespace Ardex.Sync.EntityMapping
{
    /// <summary>
    /// Provides mapping for the record version
    /// value essential to the sync engine.
    /// </summary>
    public delegate TVersion SyncEntityVersionMapping<TEntity, TVersion>(TEntity entity);
}