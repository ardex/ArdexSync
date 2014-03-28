using System;

namespace Ardex.Collections.Generic
{
    /// <summary>
    /// Operation contract for a common container
    /// with elements identified by their unique keys.
    /// </summary>
    public interface IKeyRepository<TKey, TEntity> : IRepository<TEntity>
    {
        /// <summary>
        /// Delegate used to extract unique keys from collection elements.
        /// </summary>
        Func<TEntity, TKey> KeySelector { get; }

        /// <summary>
        /// Returns the element with the specified
        /// key, or the default value for type.
        /// </summary>
        TEntity Find(TKey key);

        /// <summary>
        /// Returns the element with the specified
        /// key, or the default value for type.
        /// </summary>
        bool TryFind(TKey key, out TEntity entity);
    }
}