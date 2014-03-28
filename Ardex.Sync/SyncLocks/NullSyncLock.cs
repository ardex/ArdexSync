using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.Sync.SyncLocks
{
    public struct NullSyncLock : ISyncLock
    {
        public IDisposable ReadLock()
        {
            return Disposables.Null;
        }

        public IDisposable WriteLock()
        {
            return Disposables.Null;
        }

        public void Dispose()
        {
            
        }
    }
}
