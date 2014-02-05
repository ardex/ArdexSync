using Ardex.Sync.ChangeTracking;

namespace Ardex.Sync.Providers.ChangeBased
{
    /// <summary>
    /// Links the change history entry to its actual entity.
    /// </summary>
    public class Change<TEntity>
    {
        /// <summary>
        /// Gets the ChangeHistory specified when this instance was created.
        /// </summary>
        public IChangeHistory ChangeHistory { get; private set; }

        /// <summary>
        /// Gets the entity specified when this instance was created.
        /// </summary>
        public TEntity Entity { get; private set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public Change(IChangeHistory changeHistory, TEntity entity)
        {
            this.ChangeHistory = changeHistory;
            this.Entity = entity;
        }
    }
}
