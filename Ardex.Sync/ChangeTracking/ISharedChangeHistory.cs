namespace Ardex.Sync.ChangeTracking
{
    /// <summary>
    /// Change history entry which tracks
    /// changes in multiple articles.
    /// </summary>
    public interface ISharedChangeHistory : IChangeHistory
    {
        /// <summary>
        /// Unique ID of the sync article that
        /// this change history entry relates to.
        /// </summary>
        SyncID ArticleID { get; set; }
    }
}