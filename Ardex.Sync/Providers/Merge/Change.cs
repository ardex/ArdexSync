using Ardex.Sync.ChangeTracking;

namespace Ardex.Sync.Providers.Merge
{
    public static class Change
    {
        public static Change<TEntity, TChangeHistory> Create<TEntity, TChangeHistory>(TEntity entity, TChangeHistory changeHistory)
        {
            return new Change<TEntity, TChangeHistory>(entity, changeHistory);
        }
    }

    /// <summary>
    /// Links the change history entry to its actual entity.
    /// </summary>
    public class Change<TEntity, TChangeHistory>
    {
        /// <summary>
        /// Gets the ChangeHistory specified when this instance was created.
        /// </summary>
        public TChangeHistory ChangeHistory { get; private set; }

        /// <summary>
        /// Gets the entity specified when this instance was created.
        /// </summary>
        public TEntity Entity { get; private set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public Change(TEntity entity, TChangeHistory changeHistory)
        {
            this.ChangeHistory = changeHistory;
            this.Entity = entity;
        }
    }
}
