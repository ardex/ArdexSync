using System;
using System.Collections;
using System.Collections.Generic;

namespace Ardex.Collections.Generic
{
    /// <summary>
    /// Facilitates the creation of generic proxy repositories.
    /// </summary>
    public static class ProxyRepository
    {
        /// <summary>
        /// Create a new ProxyRepository from the given collection of entities.
        /// </summary>
        public static ProxyRepository<T> Create<T>(IRepository<T> entities)
        {
            return new ProxyRepository<T>(entities);
        }
    }

    /// <summary>
    /// Repository which uses another repository as its data source
    /// and forwards any events raised by the inner repository.
    /// Fully supports adding, updating and deleting entities and raises
    /// events when the underlying collection is modified.
    /// </summary>
    public class ProxyRepository<TEntity> : IRepository<TEntity>
    {
        /// <summary>
        /// Underlying repository.
        /// </summary>
        private readonly IRepository<TEntity> __innerRepository;

        /// <summary>
        /// True if this repo owns the underlying repository
        /// and is threfore responsible for disposing it.
        /// </summary>
        private readonly bool DisposeInnerRepository;

        /// <summary>
        /// Used to detect redundant calls to Dispose().
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Gets the inner repository.
        /// </summary>
        protected IRepository<TEntity> InnerRepository
        {
            get { return __innerRepository; }
        }

        /// <summary>
        /// Gets or sets the value which determines
        /// whether the EntityInserted/Updated/Deleted
        /// events of the inner repository are being
        /// forwarded as EntityInserted/Updated/Deleted
        /// events by this instance.
        /// </summary>
        public virtual bool ForwardEvents { get; set; }

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
        /// Initializes a new instance of the <see cref="Ardex.Collections.ProxyRepository`1"/> class
        /// with an empty ListRepository as its backing store.
        /// </summary>
        public ProxyRepository()
        {
            __innerRepository = new ListRepository<TEntity>();
            this.DisposeInnerRepository = true;

            this.SubscribeToInnerEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ardex.Collections.ProxyRepository`1"/> class
        /// with a ListRepository pre-populated with the given entities as its backing store.
        /// </summary>
        public ProxyRepository(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException("entities");

            __innerRepository = new ListRepository<TEntity>(entities);
            this.DisposeInnerRepository = true;

            this.SubscribeToInnerEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ardex.Collections.ProxyRepository`1"/> class.
        /// </summary>
        /// <param name='repository'>
        /// Repository.
        /// </param>
        /// <exception cref='ArgumentNullException'>
        /// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
        /// </exception>
        public ProxyRepository(IRepository<TEntity> repository)
        {
            if (repository == null)
                throw new ArgumentNullException("repository");

            __innerRepository = repository;
            this.DisposeInnerRepository = false;

            this.SubscribeToInnerEvents();
        }

        private void SubscribeToInnerEvents()
        {
            this.InnerRepository.EntityInserted += this.OnInnerEntityInserted;
            this.InnerRepository.EntityUpdated += this.OnInnerEntityUpdated;
            this.InnerRepository.EntityDeleted += this.OnInnerEntityDeleted;
        }

        private void UnsubscribeFromInnerEvents()
        {
            this.InnerRepository.EntityInserted -= this.OnInnerEntityInserted;
            this.InnerRepository.EntityUpdated -= this.OnInnerEntityUpdated;
            this.InnerRepository.EntityDeleted -= this.OnInnerEntityDeleted;
        }

        protected virtual void OnInnerEntityInserted(TEntity entity)
        {
            if (this.ForwardEvents &&
                this.EntityInserted != null)
            {
                this.EntityInserted(entity);
            }
        }

        protected virtual void OnInnerEntityUpdated(TEntity entity)
        {
            if (this.ForwardEvents &&
                this.EntityUpdated != null)
            {
                this.EntityUpdated(entity);
            }
        }

        protected virtual void OnInnerEntityDeleted(TEntity entity)
        {
            if (this.ForwardEvents &&
                this.EntityDeleted != null)
            {
                this.EntityDeleted(entity);
            }
        }

        /// <summary>
        /// Gets the number of entities in the repository.
        /// </summary>
        public virtual int Count
        {
            get
            {
                this.ThrowIfDisposed();

                return this.InnerRepository.Count;
            }
        }

        /// <summary>
        /// Inserts the specified entity.
        /// </summary>
        public virtual void Insert(TEntity entity)
        {
            this.ThrowIfDisposed();
            this.InnerRepository.Insert(entity);

            if (!this.ForwardEvents)
            {
                if (this.EntityInserted != null)
                {
                    this.EntityInserted(entity);
                }
            }
        }

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        public virtual void Update(TEntity entity)
        {
            this.ThrowIfDisposed();
            this.InnerRepository.Update(entity);

            if (!this.ForwardEvents)
            {
                if (this.EntityUpdated != null)
                {
                    this.EntityUpdated(entity);
                }
            }
        }

        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        public virtual void Delete(TEntity entity)
        {
            this.ThrowIfDisposed();
            this.InnerRepository.Delete(entity);

            if (!this.ForwardEvents)
            {
                if (this.EntityDeleted != null)
                {
                    this.EntityDeleted(entity);
                }
            }
        }

        #region IEnumerable implementation

        /// <summary>
        /// Gets the generic enumerator.
        /// </summary>
        public virtual IEnumerator<TEntity> GetEnumerator()
        {
            this.ThrowIfDisposed();

            return this.InnerRepository.GetEnumerator();
        }

        /// <summary>
        /// Gets the non-generic weakly typed enumerator (explicit).
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            this.ThrowIfDisposed();

            return this.InnerRepository.GetEnumerator();
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
        /// Releases all resource used by the <see cref="Ardex.Collections.ProxyRepository`1"/> object.
        /// </summary>
        /// <remarks>
        /// Call <see cref="Dispose"/> when you are finished using the <see cref="Ardex.Collections.ProxyRepository`1"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="Ardex.Collections.ProxyRepository`1"/> in an unusable state.
        /// After calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="Ardex.Collections.ProxyRepository`1"/> so the garbage collector can reclaim the memory that the
        /// <see cref="Ardex.Collections.ProxyRepository`1"/> was occupying.
        /// </remarks>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resource used by the <see cref="Ardex.Collections.ProxyRepository`1"/> object.
        /// </summary>
        /// <remarks>
        /// Call <see cref="Dispose"/> when you are finished using the <see cref="Ardex.Collections.ProxyRepository`1"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="Ardex.Collections.ProxyRepository`1"/> in an unusable state.
        /// After calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="Ardex.Collections.ProxyRepository`1"/> so the garbage collector can reclaim the memory that the
        /// <see cref="Ardex.Collections.ProxyRepository`1"/> was occupying.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                this.UnsubscribeFromInnerEvents();

                this.EntityInserted = null;
                this.EntityUpdated = null;
                this.EntityDeleted = null;

                if (this.DisposeInnerRepository)
                {
                    this.InnerRepository.Dispose();
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~ProxyRepository()
        {
            this.Dispose(false);
        }

        #endregion
    }
}