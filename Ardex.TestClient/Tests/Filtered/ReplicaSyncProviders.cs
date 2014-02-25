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
        public ChangeHistorySyncProvider<InspectionCriteria, IChangeHistory> InspectionCriteria { get; private set; }
        public ChangeHistorySyncProvider<InspectionObservation, IChangeHistory> InspectionObservation { get; private set; }
        public ChangeHistorySyncProvider<InspectionValue, IChangeHistory> InspectionValue { get; private set; }
        public ChangeHistorySyncProvider<ShortList, IChangeHistory> ShortList { get; private set; }
        public ChangeHistorySyncProvider<ShortListItem, IChangeHistory> ShortListItem { get; private set; }
        public ChangeHistorySyncProvider<ShortListPermission, IChangeHistory> ShortListPermission { get; private set; }

        public ReplicaSyncProviders(Replica replica, bool cleanUpMetadata, SyncConflictStrategy conflictStrategy)
        {
            this.Replica = replica;

            // Set up providers.
            this.InspectionCriteria = new ChangeHistorySyncProvider<InspectionCriteria, IChangeHistory>(
                this.Replica.ReplicaInfo, this.Replica.InspectionCriteria, this.Replica.ChangeHistory, c => c.EntityGuid, () => new ChangeHistory()) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

            this.InspectionObservation = new ChangeHistorySyncProvider<InspectionObservation, IChangeHistory>(
                this.Replica.ReplicaInfo, this.Replica.InspectionObservations, this.Replica.ChangeHistory, c => c.EntityGuid, () => new ChangeHistory()) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

            this.InspectionValue = new ChangeHistorySyncProvider<InspectionValue, IChangeHistory>(
                this.Replica.ReplicaInfo, this.Replica.InspectionValues, this.Replica.ChangeHistory, c => c.EntityGuid, () => new ChangeHistory()) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

            this.ShortList = new ChangeHistorySyncProvider<ShortList, IChangeHistory>(
                this.Replica.ReplicaInfo, this.Replica.ShortLists, this.Replica.ChangeHistory, c => c.EntityGuid, () => new ChangeHistory()) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

            this.ShortListItem = new ChangeHistorySyncProvider<ShortListItem, IChangeHistory>(
                this.Replica.ReplicaInfo, this.Replica.ShortListItems, this.Replica.ChangeHistory, c => c.EntityGuid, () => new ChangeHistory()) {
                    CleanUpMetadata = cleanUpMetadata,
                    ConflictStrategy = conflictStrategy
                };

            this.ShortListPermission = new ChangeHistorySyncProvider<ShortListPermission, IChangeHistory>(
                this.Replica.ReplicaInfo, this.Replica.ShortListPermissions, this.Replica.ChangeHistory, c => c.EntityGuid, () => new ChangeHistory()) {
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
