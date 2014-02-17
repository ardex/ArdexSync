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

        // Change tracking.
        public SyncRepository<IChangeHistory> ChangeHistory { get; private set; }

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

            // Change tracking.
            this.ChangeHistory = new SyncRepository<IChangeHistory>();

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
            public ChangeHistorySyncProvider<InspectionCriteria> InspectionCriteria { get; private set; }
            public ChangeHistorySyncProvider<InspectionObservation> InspectionObservation { get; private set; }
            public ChangeHistorySyncProvider<InspectionValue> InspectionValue { get; private set; }
            public ChangeHistorySyncProvider<ShortList> ShortList { get; private set; }
            public ChangeHistorySyncProvider<ShortListItem> ShortListItem { get; private set; }

            public ReplicaSyncProviders(Replica replica, bool cleanUpMetadata, SyncConflictStrategy conflictStrategy)
            {
                this.Replica = replica;

                // Set up providers.
                this.InspectionCriteria = new ChangeHistorySyncProvider<InspectionCriteria>(
                    this.Replica.ReplicaInfo, this.Replica.InspectionCriteria, this.Replica.ChangeHistory, c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

                this.InspectionObservation = new ChangeHistorySyncProvider<InspectionObservation>(
                    this.Replica.ReplicaInfo, this.Replica.InspectionObservations, this.Replica.ChangeHistory, c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

                this.InspectionValue = new ChangeHistorySyncProvider<InspectionValue>(
                    this.Replica.ReplicaInfo, this.Replica.InspectionValues, this.Replica.ChangeHistory, c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

                this.ShortList = new ChangeHistorySyncProvider<ShortList>(
                    this.Replica.ReplicaInfo, this.Replica.ShortLists, this.Replica.ChangeHistory, c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

                this.ShortListItem = new ChangeHistorySyncProvider<ShortListItem>(
                    this.Replica.ReplicaInfo, this.Replica.ShortListItems, this.Replica.ChangeHistory, c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

                // Unique article IDs.
                this.InspectionCriteria.ArticleID = 1;
                this.InspectionObservation.ArticleID = 2;
                this.InspectionValue.ArticleID = 3;
                this.ShortList.ArticleID = 4;
                this.ShortListItem.ArticleID = 5;
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
