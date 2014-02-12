using System;

namespace Ardex.Sync.EntityMapping
{
    /// <summary>
    /// Provides mapping for globally unique entity
    /// identifier essential to two-way synchronisation.
    /// </summary>
    public delegate Guid SyncGuidMapping<TEntity>(TEntity entity);
}