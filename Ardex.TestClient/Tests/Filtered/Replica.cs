using System;
using Ardex.Sync;
using Ardex.Sync.ChangeTracking;
using Ardex.Sync.Providers;
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

            // Providers.
            this.SyncProviders = new ReplicaSyncProviders(this, cleanUpMetadata, conflictStrategy);
        }

        public void Dispose()
        {
            this.SyncProviders.Dispose();
        }

        public class ReplicaSyncProviders : IDisposable
        {
            // Params.
            public Replica Replica { get; private set; }

            // Sync providers.
            public ExclusiveChangeHistorySyncProvider<InspectionCriteria> InspectionCriteria { get; private set; }
            public ExclusiveChangeHistorySyncProvider<InspectionObservation> InspectionObservation { get; private set; }
            public ExclusiveChangeHistorySyncProvider<InspectionValue> InspectionValue { get; private set; }
            public ExclusiveChangeHistorySyncProvider<ShortList> ShortList { get; private set; }
            public ExclusiveChangeHistorySyncProvider<ShortListItem> ShortListItem { get; private set; }

            public ReplicaSyncProviders(Replica replica, bool cleanUpMetadata, SyncConflictStrategy conflictStrategy)
            {
                this.Replica = replica;

                // Set up providers.
                this.InspectionCriteria = new ExclusiveChangeHistorySyncProvider<InspectionCriteria>(
                    this.Replica.ReplicaInfo, this.Replica.InspectionCriteria, new SyncRepository<IChangeHistory>(), c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

                this.InspectionObservation = new ExclusiveChangeHistorySyncProvider<InspectionObservation>(
                    this.Replica.ReplicaInfo, this.Replica.InspectionObservations, new SyncRepository<IChangeHistory>(), c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

                this.InspectionValue = new ExclusiveChangeHistorySyncProvider<InspectionValue>(
                    this.Replica.ReplicaInfo, this.Replica.InspectionValues, new SyncRepository<IChangeHistory>(), c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

                this.ShortList = new ExclusiveChangeHistorySyncProvider<ShortList>(
                    this.Replica.ReplicaInfo, this.Replica.ShortLists, new SyncRepository<IChangeHistory>(), c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

                this.ShortListItem = new ExclusiveChangeHistorySyncProvider<ShortListItem>(
                    this.Replica.ReplicaInfo, this.Replica.ShortListItems, new SyncRepository<IChangeHistory>(), c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };
            }

            public void Dispose()
            {
                this.InspectionCriteria.Dispose();
                this.InspectionObservation.Dispose();
                this.InspectionValue.Dispose();
                this.ShortListItem.Dispose();
                this.ShortList.Dispose();
            }
        }
    }
}
