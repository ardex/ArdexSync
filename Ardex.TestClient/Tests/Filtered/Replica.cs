using System;

using Ardex.Sync;
using Ardex.Sync.Providers;

using Ardex.TestClient.Tests.Filtered.Entities;

namespace Ardex.TestClient.Tests.Filtered
{
    public class Replica : IDisposable
    {
        public SyncReplicaInfo ReplicaInfo { get; set; }
        public ExclusiveChangeHistorySyncProvider<InspectionCriteria> InspectionCriteria { get; private set; }
        public ExclusiveChangeHistorySyncProvider<InspectionObservation> InspectionObservations { get; private set; }
        public ExclusiveChangeHistorySyncProvider<InspectionValue> InspectionValues { get; private set; }
        public ExclusiveChangeHistorySyncProvider<ShortList> ShortLists { get; private set; }
        public ExclusiveChangeHistorySyncProvider<ShortListItem> ShortListItems { get; private set; }

        public Replica(SyncReplicaInfo replicaInfo, bool cleanUpMetadata, SyncConflictStrategy conflictStrategy)
        {
            this.ReplicaInfo = replicaInfo;

            // Set up providers.
            this.InspectionCriteria = new ExclusiveChangeHistorySyncProvider<InspectionCriteria>(replicaInfo, c => c.EntityGuid)
            {
                CleanUpMetadata = cleanUpMetadata,
                ConflictStrategy = conflictStrategy
            };

            this.InspectionObservations = new ExclusiveChangeHistorySyncProvider<InspectionObservation>(replicaInfo, c => c.EntityGuid)
            {
                CleanUpMetadata = cleanUpMetadata,
                ConflictStrategy = conflictStrategy
            };

            this.InspectionValues = new ExclusiveChangeHistorySyncProvider<InspectionValue>(replicaInfo, c => c.EntityGuid)
            {
                CleanUpMetadata = cleanUpMetadata,
                ConflictStrategy = conflictStrategy
            };

            this.ShortLists = new ExclusiveChangeHistorySyncProvider<ShortList>(replicaInfo, c => c.EntityGuid)
            {
                CleanUpMetadata = cleanUpMetadata,
                ConflictStrategy = conflictStrategy
            };

            this.ShortListItems = new ExclusiveChangeHistorySyncProvider<ShortListItem>(replicaInfo, c => c.EntityGuid)
            {
                CleanUpMetadata = cleanUpMetadata,
                ConflictStrategy = conflictStrategy
            };
        }

        public void Dispose()
        {
            this.InspectionCriteria.Dispose();
            this.InspectionObservations.Dispose();
            this.InspectionValues.Dispose();
            this.ShortListItems.Dispose();
            this.ShortLists.Dispose();
        }
    }
}
