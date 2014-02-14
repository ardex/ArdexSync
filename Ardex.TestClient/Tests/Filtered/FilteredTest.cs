using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ardex.Reflection;

using Ardex.Sync;
using Ardex.Sync.ChangeTracking;
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
            this.InspectionCriteria = new ExclusiveChangeHistorySyncProvider<InspectionCriteria>(replicaInfo, c => c.EntityGuid) {
                CleanUpMetadata = cleanUpMetadata,
                ConflictStrategy = conflictStrategy
            };

            this.InspectionObservations = new ExclusiveChangeHistorySyncProvider<InspectionObservation>(replicaInfo, c => c.EntityGuid) {
                CleanUpMetadata = cleanUpMetadata,
                ConflictStrategy = conflictStrategy
            };

            this.InspectionValues = new ExclusiveChangeHistorySyncProvider<InspectionValue>(replicaInfo, c => c.EntityGuid) {
                CleanUpMetadata = cleanUpMetadata,
                ConflictStrategy = conflictStrategy
            };

            this.ShortLists = new ExclusiveChangeHistorySyncProvider<ShortList>(replicaInfo, c => c.EntityGuid) {
                CleanUpMetadata = cleanUpMetadata,
                ConflictStrategy = conflictStrategy
            };

            this.ShortListItems = new ExclusiveChangeHistorySyncProvider<ShortListItem>(replicaInfo, c => c.EntityGuid) {
                CleanUpMetadata = cleanUpMetadata,
                ConflictStrategy = conflictStrategy
            };

            //this.InspectionCriteria.EntityTypeMapping = new TypeMapping<InspectionCriteria>().Exclude(c => c.CriteriaID);
            //this.InspectionObservations.EntityTypeMapping = new TypeMapping<InspectionObservation>().Exclude(o => o.ObservationID);
            //this.InspectionValues.EntityTypeMapping = new TypeMapping<InspectionValue>().Exclude(c => c.ValueID);
            //this.ShortLists.EntityTypeMapping = new TypeMapping<ShortList>().Exclude(l => l.ShortListID);
            //this.ShortListItems.EntityTypeMapping = new TypeMapping<ShortListItem>().Exclude(i => i.ShortListItemID);
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

    public class FilteredTest : IDisposable
    {
        public Replica Server { get; private set; }
        public Replica Client1 { get; private set; }
        public Replica Client2 { get; private set; }

        // Sync operations.
        public SyncOperation Client1Sync { get; private set; }
        public SyncOperation Client2Sync { get; private set; }

        // Set up.
        public FilteredTest()
        {
            // Replica ID's.
            var serverInfo  = new SyncReplicaInfo(255, "Server");
            var client1Info = new SyncReplicaInfo(1, "Client 1");
            var client2Info = new SyncReplicaInfo(2, "Client 2");

            this.Server = new Replica(serverInfo, false, SyncConflictStrategy.Winner);
            this.Client1 = new Replica(client1Info, true, SyncConflictStrategy.Loser);
            this.Client2 = new Replica(client2Info, true, SyncConflictStrategy.Loser);

            //// Chain sync operations to produce an upload/download chain.
            //var client1Upload   = SyncOperation.Create(this.Client1, this.Server).Filtered(ChangeHistoryFilters.Exclusive);
            //var client1Download = SyncOperation.Create(this.Server, this.Client1).Filtered(ChangeHistoryFilters.Exclusive);
            //var client2Upload   = SyncOperation.Create(this.Client2, this.Server).Filtered(ChangeHistoryFilters.Exclusive);
            //var client2Download = SyncOperation.Create(this.Server, this.Client2).Filtered(ChangeHistoryFilters.Exclusive);

            //// Chain uploads and downloads to produce complete sync sessions.
            //this.Client1Sync = SyncOperation.Chain(client1Upload, client1Download);
            //this.Client2Sync = SyncOperation.Chain(client2Upload, client2Download);
        }

        public void Dispose()
        {
            this.Server.Dispose();
            this.Client1.Dispose();
            this.Client2.Dispose();
        }
    }
}
