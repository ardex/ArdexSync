using System;
using System.Collections;
using System.Collections.Generic;

namespace Ardex.Collections.Generic
{
    /// <summary>
    /// Operation contract for a common
    /// container for a specific entity type.
    /// </summary>
    public interface IRepository<TEntity> : IEnumerable<TEntity>, IEnumerable, IDisposable
    {
        /// <summary>
        /// The number of items in the respository.
        /// </summary>
        int Count { get; }

        /// <summary>
		/// Inserts the specified entity.
		/// </summary>
		void Insert(TEntity entity);
		
		/// <summary>
		/// Updates the specified entity.
		/// </summary>
		void Update(TEntity entity);
		
		/// <summary>
		/// Deletes the specified entity.
		/// </summary>
		void Delete(TEntity entity);

        /// <summary>
        /// Occurs when entity is inserted.
        /// </summary>
        event Action<TEntity> EntityInserted;
        
        /// <summary>
        /// Occurs when entity is updated.
        /// </summary>
        event Action<TEntity> EntityUpdated;
       
        /// <summary>
        /// Occurs when entity is deleted.
        /// </summary>
        event Action<TEntity> EntityDeleted;
    }
}