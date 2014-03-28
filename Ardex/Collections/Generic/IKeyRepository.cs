using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Performs an inner join between the given
        /// items and the entities in this repository.
        /// </summary>
        IEnumerable<TResult> Join<TInner, TResult>(
            IEnumerable<TInner> items, Func<TInner, TKey> keySelector, Func<TEntity, TInner, TResult> resultSelector);

        /// <summary>
        /// Performs an outer join between the given
        /// items and the entities in this repository.
        /// </summary>
        IEnumerable<TResult> OuterJoin<TInner, TResult>(
            IEnumerable<TInner> items, Func<TInner, TKey> keySelector, Func<TEntity, TInner, TResult> resultSelector);
    }
}