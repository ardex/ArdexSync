using System;

namespace Ardex.Sync.EntityMapping
{
    /// <summary>
    /// Provides mapping for entity members essential to two-way synchronisation.
    /// </summary>
    public class UniqueIdMapping<TEntity>
    {
        private readonly Func<TEntity, Guid> __getter;

        // Constructors.
        public UniqueIdMapping(Func<TEntity, Guid> getter) { __getter = getter; }
        public UniqueIdMapping(Func<TEntity, string> getter) { __getter = obj => Guid.Parse(getter(obj)); }

        /// <summary>
        /// Returns the unique ID value of the given entity.
        /// </summary>
        public Guid Get(TEntity entity)
        {
            return __getter(entity);
        }

        public static implicit operator UniqueIdMapping<TEntity>(Func<TEntity, Guid> getter)
        {
            return new UniqueIdMapping<TEntity>(getter);
        }
    }
}