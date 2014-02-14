using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ardex.Collections.Generic
{
	/// <summary>
    /// Facilitates the creation of generic list repositories.
    /// </summary>
    public static class ListRepository
    {
		/// <summary>
		/// Create a new ListRepository from the given collection of entities.
		/// </summary>
        public static ListRepository<T> Create<T>(IEnumerable<T> entities)
        {
            return new ListRepository<T>(entities);
        }
    }
	
    /// <summary>
    /// Repository which uses a generic list of entities as its data source.
    /// Fully supports adding, updating and deleting entities and raises
    /// events when the underlying collection is modified.
    /// </summary>
    public class ListRepository<TEntity> : IRepository<TEntity>
    {
		/// <summary>
		/// Underlying list of entities.
		/// </summary>
        private readonly List<TEntity> __entities;

		/// <summary>
		/// Used to detect redundant calls to Dispose().
		/// </summary>
		private bool _disposed;

        /// <summary>
        /// Gets the <see cref="Ardex.Collections.ListRepository`1"/> at the specified index.
        /// </summary>
        /// <param name='index'>
        /// Index.
        /// </param>
        public virtual TEntity this[int index]
        {
            get
            {
                return this.Entities[index];
            }
        }

        /// <summary>
        /// Gets the underlying collection of entities.
        /// </summary>
        private List<TEntity> Entities
        {
            get { return __entities; }
        }
		
		/// <summary>
		/// Occurs when entity is inserted.
		/// </summary>
		public event Action<TEntity> EntityInserted;
		
		/// <summary>
		/// Occurs when entity is updated.
		/// </summary>
		public event Action<TEntity> EntityUpdated;
		
		/// <summary>
		/// Occurs when entity is deleted.
		/// </summary>
		public event Action<TEntity> EntityDeleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ardex.Collections.ListRepository`1"/> class.
        /// </summary>
        public ListRepository()
        {
            __entities = new List<TEntity>();
        }
        
		/// <summary>
		/// Initializes a new instance of the <see cref="Ardex.Collections.ListRepository`1"/> class.
		/// </summary>
		/// <param name='entities'>
		/// Entities.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
		/// </exception>
        public ListRepository(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException("entities");
            
            __entities = entities.ToList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ardex.Collections.ListRepository`1"/> class.
        /// </summary>
        /// <param name='entities'>
        /// Entities.
        /// </param>
        /// <exception cref='ArgumentNullException'>
        /// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
        /// </exception>
        public ListRepository(params TEntity[] entities)
        {
            if (entities == null)
                throw new ArgumentNullException("entities");
            
            __entities = entities.ToList();
        }
        
		/// <summary>
		/// Gets the number of entities in the repository.
		/// </summary>
        public virtual int Count
        {
            get
			{
				this.ThrowIfDisposed();
				
				return this.Entities.Count;
			}   
        }
		
		/// <summary>
		/// Insert the specified entity.
		/// </summary>
		public virtual void Insert(TEntity entity)
		{
			this.ThrowIfDisposed();
			this.Entities.Add(entity);
			
			if (this.EntityInserted != null)
            {
                this.EntityInserted(entity);
            }
		}

		/// <summary>
		/// Update the specified entity.
		/// </summary>
		public virtual void Update(TEntity entity)
		{
			this.ThrowIfDisposed();
			
			if (this.EntityUpdated != null)
            {
                this.EntityUpdated(entity);
            }
		}
		
		/// <summary>
		/// Delete the specified entity.
		/// </summary>
		public virtual void Delete(TEntity entity)
		{
			this.ThrowIfDisposed();
			
			this.Entities.Remove(entity);
			
			if (this.EntityDeleted != null)
            {
                this.EntityDeleted(entity);
            }
		}

        #region IEnumerable implementation
        
		/// <summary>
		/// Gets the generic enumerator.
		/// </summary>
        public virtual IEnumerator<TEntity> GetEnumerator()
        {
			this.ThrowIfDisposed();
			
            return this.Entities.GetEnumerator();
        }
		
		/// <summary>
		/// Gets the non-generic weakly typed enumerator (explicit).
		/// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
			this.ThrowIfDisposed();
			
            return this.Entities.GetEnumerator();
        }
        
        #endregion
		
		#region IDisposable implementation
		
		/// <summary>
		/// Throws the ObjectDisposedException if this instance has been disposed.
		/// </summary>
		/// <exception cref='ObjectDisposedException'>
		/// Is thrown when an operation is performed on a disposed object.
		/// </exception>
		protected void ThrowIfDisposed()
		{
			if (_disposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }
		}
		
		/// <summary>
		/// Releases all resource used by the <see cref="Ardex.Collections.ListRepository`1"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="Ardex.Collections.ListRepository`1"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Ardex.Collections.ListRepository`1"/> in an unusable state.
		/// After calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="Ardex.Collections.ListRepository`1"/> so the garbage collector can reclaim the memory that the
		/// <see cref="Ardex.Collections.ListRepository`1"/> was occupying.
		/// </remarks>
		public void Dispose()
		{
            this.Dispose(true);
            GC.SuppressFinalize(this);
		}

        /// <summary>
        /// Releases all resource used by the <see cref="Ardex.Collections.ListRepository`1"/> object.
        /// </summary>
        /// <remarks>
        /// Call <see cref="Dispose"/> when you are finished using the <see cref="Ardex.Collections.ListRepository`1"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="Ardex.Collections.ListRepository`1"/> in an unusable state.
        /// After calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="Ardex.Collections.ListRepository`1"/> so the garbage collector can reclaim the memory that the
        /// <see cref="Ardex.Collections.ListRepository`1"/> was occupying.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                this.EntityInserted = null;
                this.EntityUpdated = null;
                this.EntityDeleted = null;
            }

            _disposed = true;
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~ListRepository()
        {
            this.Dispose(false);
        }
		
		#endregion
	}
}

