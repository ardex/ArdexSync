using System;

namespace Ardex.Sync.EntityMapping
{
    /// <summary>
    /// Provides mapping for entity members essential to two-way synchronisation.
    /// </summary>
    public class TimestampMapping<TEntity>
    {
        private readonly Func<TEntity, Timestamp> __getter;

        // Constructors.
        public TimestampMapping(Func<TEntity, Timestamp> getter) { __getter = getter; }
        public TimestampMapping(Func<TEntity, string> getter)    { __getter = obj => new Timestamp(getter(obj)); }
        public TimestampMapping(Func<TEntity, byte[]> getter)    { __getter = obj => new Timestamp(getter(obj)); }

        /// <summary>
        /// Returns the timestamp value of the given entity.
        /// </summary>
        internal Timestamp Get(TEntity entity)
        {
            return __getter(entity);
        }
    }
}

