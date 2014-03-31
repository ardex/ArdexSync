using System;
using System.Threading;

using Ardex.Caching;

namespace Ardex.Collections.Generic
{
    /// <summary>
    /// Provides a lazy thread-safe view of the
    /// repository which is automatically invalidated
    /// in response to changes in the repository.
    /// </summary>
    public class RepositoryView<TEntity, TView> : LazyCache<TView>, IDisposable where TView : class
    {
        /// <summary>
        /// Repository specified when this instance was created.
        /// </summary>
        public IRepository<TEntity> Repository { get; private set; }

        /// <summary>
        /// Creates a new instance of RepositoryView.
        /// </summary>
        public RepositoryView(IRepository<TEntity> repository, Func<TView> valueFactory)
            : base(valueFactory)
        {
            this.Repository = repository;

            // Events.
            this.Repository.EntityInserted += this.RepositoryChanged;
            this.Repository.EntityUpdated += this.RepositoryChanged;
            this.Repository.EntityDeleted += this.RepositoryChanged;
        }

        /// <summary>
        /// Invalidates the view.
        /// </summary>
        private void RepositoryChanged(TEntity _)
        {
            this.Invalidate();
        }

        /// <summary>
        /// Cleans up resources used by this view
        /// and allows the GC to do its job.
        /// </summary>
        public void Dispose()
        {
            this.Repository.EntityInserted -= this.RepositoryChanged;
            this.Repository.EntityUpdated -= this.RepositoryChanged;
            this.Repository.EntityDeleted -= this.RepositoryChanged;
        }
    }
}