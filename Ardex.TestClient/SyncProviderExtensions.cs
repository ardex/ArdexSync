using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ardex.Sync;
using Ardex.Sync.ChangeTracking;
using Ardex.Sync.Providers;

namespace Ardex.TestClient
{
    public static class SyncProviderExtensions
    {
        private static readonly Dictionary<object, Guid> LastSequentialIDs = new Dictionary<object, Guid>();

        public static Guid NewSequentialID<TEntity, TKey, TVersion>(this SyncProvider<TEntity, TKey, TVersion> syncProvider) where TEntity : class
        {
            lock (LastSequentialIDs)
            {
                var lastSeenGuid = default(Guid);

                LastSequentialIDs.TryGetValue(syncProvider, out lastSeenGuid);

                var gb = new SyncGuidBuilder(lastSeenGuid);

                gb.ReplicaID = syncProvider.ReplicaInfo.ReplicaID;
                gb.EntityID++;

                return LastSequentialIDs[syncProvider] = gb.ToGuid();
            }
        }
    }
}
