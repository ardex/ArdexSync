using System.Collections.Generic;

namespace Ardex.Sync
{
    public interface ISyncAnchor<TVersion>
    {
        /// <summary>
        /// Retrieves the last anchor containing
        /// this replica's latest change knowledge.
        /// It is used by the other side in order to detect
        /// the change delta that needs to be transferred
        /// and to detect and resolve conflicts.
        /// </summary>
        Dictionary<SyncID, TVersion> LastAnchor();
    }
}
