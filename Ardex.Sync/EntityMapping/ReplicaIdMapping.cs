using System;

namespace Ardex.Sync.EntityMapping
{
    /// <summary>
    /// Provides mapping for entity members essential to two-way synchronisation.
    /// </summary>
    public class ReplicaIdMapping<TEntity>
    {
        private readonly Func<TEntity, int> __getter;

        // Constructors.
        public ReplicaIdMapping(Func<TEntity, int> getter) { __getter = getter; }

        /// <summary>
        /// Returns the replica ID value of the given entity.
        /// </summary>
        public int Get(TEntity entity)
        {
            return __getter(entity);
        }

        public static implicit operator ReplicaIdMapping<TEntity>(Func<TEntity, int> getter)
        {
            return new ReplicaIdMapping<TEntity>(getter);
        }
    }
}