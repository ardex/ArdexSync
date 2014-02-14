namespace Ardex.Sync
{
    /// <summary>
    /// Delegate used to generate and apply a local key for the given entity.
    /// </summary>
    public delegate void SyncEntityAction<TEntity>(TEntity entity);
}