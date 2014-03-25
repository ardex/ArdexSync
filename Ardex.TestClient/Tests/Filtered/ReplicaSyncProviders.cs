using System;

using Ardex.Sync;
using Ardex.Sync.ChangeTracking;
using Ardex.Sync.Providers;
using Ardex.TestClient.Tests.Filtered.Entities;

namespace Ardex.TestClient.Tests.Filtered
{
    public class ReplicaSyncProviders : IDisposable
    {
        // Params.
        public Replica Replica { get; private set; }

        // Sync providers.
        public ChangeHistorySyncProvider<InspectionCriteria, ChangeHistory> InspectionCriteria { get; private set; }
        public ChangeHistorySyncProvider<InspectionObservation, ChangeHistory> InspectionObservation { get; private set; }
        public ChangeHistorySyncProvider<InspectionValue, ChangeHistory> InspectionValue { get; private set; }
        public ChangeHistorySyncProvider<ShortList, ChangeHistory> ShortList { get; private set; }
        public ChangeHistorySyncProvider<ShortListItem, ChangeHistory> ShortListItem { get; private set; }
        public ChangeHistorySyncProvider<ShortListPermission, ChangeHistory> ShortListPermission { get; private set; }

        public ReplicaSyncProviders(Replica replica, bool cleanUpMetadata, SyncConflictStrategy conflictStrategy)
        {
            this.Replica = replica;

            // Set up providers.
            this.InspectionCriteria = new ChangeHistorySyncProvider<InspectionCriteria, ChangeHistory>(
                this.Replica.ReplicaInfo, this.Replica.InspectionCriteria, this.Replica.ChangeHistory, c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

            this.InspectionObservation = new ChangeHistorySyncProvider<InspectionObservation, ChangeHistory>(
                this.Replica.ReplicaInfo, this.Replica.InspectionObservations, this.Replica.ChangeHistory, c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

            this.InspectionValue = new ChangeHistorySyncProvider<InspectionValue, ChangeHistory>(
                this.Replica.ReplicaInfo, this.Replica.InspectionValues, this.Replica.ChangeHistory, c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

            this.ShortList = new ChangeHistorySyncProvider<ShortList, ChangeHistory>(
                this.Replica.ReplicaInfo, this.Replica.ShortLists, this.Replica.ChangeHistory, c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

            this.ShortListItem = new ChangeHistorySyncProvider<ShortListItem, ChangeHistory>(
                this.Replica.ReplicaInfo, this.Replica.ShortListItems, this.Replica.ChangeHistory, c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

            this.ShortListPermission = new ChangeHistorySyncProvider<ShortListPermission, ChangeHistory>(
                this.Replica.ReplicaInfo, this.Replica.ShortListPermissions, this.Replica.ChangeHistory, c => c.EntityGuid) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

            // Unique article IDs.
            this.InspectionCriteria.ArticleID = 1;
            this.InspectionObservation.ArticleID = 2;
            this.InspectionValue.ArticleID = 3;
            this.ShortList.ArticleID = 4;
            this.ShortListItem.ArticleID = 5;
            this.ShortListPermission.ArticleID = 6;
        }

        public void Dispose()
        {
            this.InspectionCriteria.Dispose();
            this.InspectionObservation.Dispose();
            this.InspectionValue.Dispose();
            this.ShortListItem.Dispose();
            this.ShortList.Dispose();
            this.ShortListPermission.Dispose();
        }
    }
}
