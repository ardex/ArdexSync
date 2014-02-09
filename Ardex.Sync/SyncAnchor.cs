using System;
using System.Collections.Generic;
using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync
{
    public static class SyncAnchor
    {
        public static SyncAnchor<TVersion> Create<TEntity, TVersion>(
            IEnumerable<TEntity> entities,
            UniqueIdMapping<TEntity> ownerReplicaIdMapping,
            Func<TEntity, TVersion> entityVersionMapping,
            IComparer<TVersion> versionComparer)
        {
            var anchor = new SyncAnchor<TVersion>();

            foreach (var entity in entities)
            {
                var entityOwnerReplicaID = ownerReplicaIdMapping == null ? default(SyncID) : ownerReplicaIdMapping.Get(entity);
                var entityVersion = entityVersionMapping(entity);
                var maxVersion = default(TVersion);

                if (!anchor.TryGetValue(entityOwnerReplicaID, out maxVersion) ||
                    versionComparer.Compare(entityVersion, maxVersion) > 0)
                {
                    anchor[entityOwnerReplicaID] = entityVersion;
                }
            }

            return anchor;
        }
    }

    public class SyncAnchor<TVersion>
    {
        private readonly List<SyncAnchorEntry<TVersion>> __entries = new List<SyncAnchorEntry<TVersion>>();

        public TVersion this[SyncID replicaID]
        {
            get
            { 
                foreach (var entry in __entries)
                {
                    if (entry.ReplicaID == replicaID)
                    {
                        return entry.MaxVersion;
                    }
                }

                return default(TVersion);
            }
            set
            {
                foreach (var entry in __entries)
                {
                    if (entry.ReplicaID == replicaID)
                    {
                        entry.MaxVersion = value;
                        return;
                    }
                }

                __entries.Add(new SyncAnchorEntry<TVersion>(replicaID) { MaxVersion = value });
            }
        }

        public bool TryGetValue(SyncID replicaID, out TVersion maxVersion)
        {
            foreach (var entry in __entries)
            {
                if (entry.ReplicaID == replicaID)
                {
                    maxVersion = entry.MaxVersion;
                    
                    return true;
                }
            }

            maxVersion = default(TVersion);

            return false;
        }
    }

    internal class SyncAnchorEntry<TVersion>
    {
        public SyncID ReplicaID { get; private set; }
        public TVersion MaxVersion { get; set; }

        public SyncAnchorEntry(SyncID replicaID)
        {
            this.ReplicaID = replicaID;
        }
    }
}
