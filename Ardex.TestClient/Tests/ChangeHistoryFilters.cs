using System.Linq;

using Ardex.Sync;
using Ardex.Sync.ChangeTracking;

namespace Ardex.TestClient.Tests
{
    public static class ChangeHistoryFilters
    {
        public static SyncFilter<Dummy, IChangeHistory> Serialization
        {
            get
            {
                // Change filter: emulate serialization/deserialization.
                // This is not necessary in real-world scenarios.
                return new SyncFilter<Dummy, IChangeHistory>(
                    changes => changes.Select(
                        c => new SyncEntityVersion<Dummy, IChangeHistory>(
                            c.Entity.Clone(),
                            new ChangeHistory(c.Version)
                        )
                    )
                );
            }
        }
    }
}
