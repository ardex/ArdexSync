//using System;

//using Ardex.Sync;
//using Ardex.Sync.ChangeTracking;

//using Ardex.TestClient.Tests.Filtered.Entities;

//namespace Ardex.TestClient.Tests.Filtered
//{
//    public class Replica : IDisposable
//    {
//        // Params.
//        public SyncReplicaInfo ReplicaInfo { get; private set; }

//        // Repositories.
//        public SyncRepository<int, InspectionCriteria> InspectionCriteria { get; private set; }
//        public SyncRepository<int, InspectionObservation> InspectionObservations { get; private set; }
//        public SyncRepository<int, InspectionValue> InspectionValues { get; private set; }
//        public SyncRepository<int, ShortList> ShortLists { get; private set; }
//        public SyncRepository<int, ShortListItem> ShortListItems { get; private set; }
//        public SyncRepository<int, ShortListPermission> ShortListPermissions { get; private set; }

//        // Change tracking.
//        public SyncRepository<int, ChangeHistory> ChangeHistory { get; private set; }

//        // Providers.
//        public ReplicaSyncProviders SyncProviders { get; private set; }

//        public Replica(SyncReplicaInfo replicaInfo, bool cleanUpMetadata, SyncConflictStrategy conflictStrategy)
//        {
//            // Params.
//            this.ReplicaInfo = replicaInfo;

//            // Repositories.
//            this.InspectionCriteria = new SyncRepository<int, InspectionCriteria>(ic => ic.CriteriaID);
//            this.InspectionObservations = new SyncRepository<int, InspectionObservation>(io => io.ObservationID);
//            this.InspectionValues = new SyncRepository<int, InspectionValue>(iv => iv.ValueID);
//            this.ShortLists = new SyncRepository<int, ShortList>(sl => sl.ShortListID);
//            this.ShortListItems = new SyncRepository<int, ShortListItem>(sli => sli.ShortListItemID);
//            this.ShortListPermissions = new SyncRepository<int, ShortListPermission>(slp => slp.PermissionID);

//            // Change tracking.
//            this.ChangeHistory = new SyncRepository<int, ChangeHistory>(ch => ch.ChangeHistoryID);

//            // Providers.
//            this.SyncProviders = new ReplicaSyncProviders(this, cleanUpMetadata, conflictStrategy);
//        }

//        public void Dispose()
//        {
//            this.InspectionCriteria.Dispose();
//            this.InspectionObservations.Dispose();
//            this.InspectionValues.Dispose();
//            this.ShortLists.Dispose();
//            this.ShortListItems.Dispose();
//            this.ShortListPermissions.Dispose();

//            this.SyncProviders.Dispose();
//        }
//    }
//}
