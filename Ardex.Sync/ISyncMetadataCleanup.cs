using System.Collections.Generic;

namespace Ardex.Sync
{
    /// <summary>
    /// Provides methods for cleaning up sync metadata.
    /// </summary>
    public interface ISyncMetadataCleanup<TChange>
    {
        /// <summary>
        /// Performs change history cleanup.
        /// </summary>
        void CleanUpSyncMetadata(IEnumerable<TChange> appliedDelta);
    }
}
