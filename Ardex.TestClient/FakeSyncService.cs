//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Ardex.Sync;
//using Ardex.Sync.ChangeBased;

//namespace Ardex.TestClient
//{
//    public class FakeSyncService : IChangeBasedSyncSource<Dummy>, IChangeBasedSyncDestination<Dummy>
//    {
//        public Guid NodeID
//        {
//            get { throw new NotImplementedException(); }
//        }

//        public IEnumerable<ChangeHistoryEntity<Dummy>> ProduceChanges(Dictionary<Guid, Timestamp> timestampsByNode, CancellationToken ct)
//        {
//            throw new NotImplementedException();
//        }

//        public SyncResult AcceptChanges(Guid nodeID, IEnumerable<ChangeHistoryEntity<Dummy>> changes, CancellationToken ct)
//        {
//            throw new NotImplementedException();
//        }

//        public Dictionary<Guid, Timestamp> LastSeenTimestampByNode()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
