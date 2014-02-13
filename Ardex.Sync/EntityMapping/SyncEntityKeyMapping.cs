using System;

namespace Ardex.Sync.EntityMapping
{
    /// <summary>
    /// Provides mapping for the unique entity
    /// identifier essential to the sync engine.
    /// </summary>
    public delegate TKey SyncEntityKeyMapping<TEntity, TKey>(TEntity entity);
}