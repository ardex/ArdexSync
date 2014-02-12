using System;

namespace Ardex.Sync.EntityMapping
{
    /// <summary>
    /// Provides mapping for globally unique entity
    /// identifier essential to two-way synchronisation.
    /// </summary>
    public class SyncGuidMapping<TEntity>
    {
        private readonly Func<TEntity, Guid> __getter;

        // Constructors.
        public SyncGuidMapping(Func<TEntity, Guid> getter) { __getter = getter; }
        public SyncGuidMapping(Func<TEntity, string> getter) { __getter = obj => Guid.Parse(getter(obj)); }

        /// <summary>
        /// Returns the unique ID value of the given entity.
        /// </summary>
        public Guid Get(TEntity entity)
        {
            return __getter(entity);
        }

        public static implicit operator SyncGuidMapping<TEntity>(Func<TEntity, Guid> getter)
        {
            return new SyncGuidMapping<TEntity>(getter);
        }
    }
}