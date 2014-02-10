using System;

namespace Ardex.Sync
{
    /// <summary>
    /// Provides mapping for entity members essential to two-way synchronisation.
    /// </summary>
    public class UniqueIdMapping<TEntity>
    {
        private readonly Func<TEntity, SyncID> __getter;

        // Constructors.
        public UniqueIdMapping(Func<TEntity, SyncID> getter) { __getter = getter; }
        public UniqueIdMapping(Func<TEntity, string> getter) { __getter = obj => new SyncID(getter(obj)); }
        public UniqueIdMapping(Func<TEntity, int> getter)    { __getter = obj => new SyncID(getter(obj).ToString()); }
        public UniqueIdMapping(Func<TEntity, Guid> getter)   { __getter = obj => new SyncID(getter(obj).ToString()); }

        /// <summary>
        /// Returns the unique ID value of the given entity.
        /// </summary>
        public SyncID Get(TEntity entity)
        {
            return __getter(entity);
        }
    }
}

