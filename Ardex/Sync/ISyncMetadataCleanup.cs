using System.Collections.Generic;

namespace Ardex.Sync
{
    public interface ISyncMetadataCleanup<TChange>
    {
        /// <summary>
        /// Performs change history cleanup.
        /// </summary>
        void CleanUpSyncMetadata(IEnumerable<TChange> appliedDelta);
    }
}
