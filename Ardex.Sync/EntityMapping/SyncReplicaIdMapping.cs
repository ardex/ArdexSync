using System;

namespace Ardex.Sync.EntityMapping
{
    /// <summary>
    /// Provides mapping for entity members essential to two-way synchronisation.
    /// </summary>
    public delegate int SyncReplicaIdMapping<TEntity>(TEntity entity);
}