using System;

using Ardex.Sync;
using Ardex.Sync.ChangeTracking;

using Ardex.TestClient.Tests.Filtered.Entities;

namespace Ardex.TestClient.Tests.Filtered
{
    public class Replica : IDisposable
    {
        // Params.
        public SyncReplicaInfo ReplicaInfo { get; private set; }

        // Repositories.
        public SyncRepository<InspectionCriteria> InspectionCriteria { get; private set; }
        public SyncRepository<InspectionObservation> InspectionObservations { get; private set; }
        public SyncRepository<InspectionValue> InspectionValues { get; private set; }
        public SyncRepository<ShortList> ShortLists { get; private set; }
        public SyncRepository<ShortListItem> ShortListItems { get; private set; }
        public SyncRepository<ShortListPermission> ShortListPermissions { get; private set; }

        // Change tracking.
        public SyncRepository<ChangeHistory> ChangeHistory { get; private set; }

        // Providers.
        public ReplicaSyncProviders SyncProviders { get; private set; }

        public Replica(SyncReplicaInfo replicaInfo, bool cleanUpMetadata, SyncConflictStrategy conflictStrategy)
        {
            // Params.
            this.ReplicaInfo = replicaInfo;

            // Repositories.
            this.InspectionCriteria = new SyncRepository<InspectionCriteria>();
            this.InspectionObservations = new SyncRepository<InspectionObservation>();
            this.InspectionValues = new SyncRepository<InspectionValue>();
            this.ShortLists = new SyncRepository<ShortList>();
            this.ShortListItems = new SyncRepository<ShortListItem>();
            this.ShortListPermissions = new SyncRepository<ShortListPermission>();

            // Change tracking.
            this.ChangeHistory = new SyncRepository<ChangeHistory>();

            // Providers.
            this.SyncProviders = new ReplicaSyncProviders(this, cleanUpMetadata, conflictStrategy);
        }

        public void Dispose()
        {
            this.InspectionCriteria.Dispose();
            this.InspectionObservations.Dispose();
            this.InspectionValues.Dispose();
            this.ShortLists.Dispose();
            this.ShortListItems.Dispose();
            this.ShortListPermissions.Dispose();

            this.SyncProviders.Dispose();
        }
    }
}
