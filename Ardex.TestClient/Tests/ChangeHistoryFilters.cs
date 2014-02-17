using System.Linq;

using Ardex.Reflection;
using Ardex.Sync;
using Ardex.Sync.ChangeTracking;

namespace Ardex.TestClient.Tests
{
    public static class ChangeHistoryFilters
    {
        /// <summary>
        /// Simulates serialization (creates shallow clones of all
        /// reference types) without actually filtering anything.
        /// </summary>
        public static SyncFilter<TEntity, IChangeHistory> Serialization<TEntity>() where TEntity : new()
        {
            var entityMapping = new TypeMapping<TEntity>();
            var changeHistoryMapping = new TypeMapping<IChangeHistory>();

            return new SyncFilter<TEntity, IChangeHistory>(
                changes => changes.Select(
                    version =>
                    {
                        var newEntity = new TEntity();
                        var newChangeHistory = (IChangeHistory)new ChangeHistory();

                        entityMapping.CopyValues(version.Entity, newEntity);
                        changeHistoryMapping.CopyValues(version.Version, newChangeHistory);

                        return SyncEntityVersion.Create(newEntity, newChangeHistory);
                    }
                )
            );
        }
    }
}
