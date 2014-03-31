using System;
using System.Threading;

namespace Ardex.Sync.SyncLocks
{
    public class MonitorSyncLock : ISyncLock
    {
        private readonly object SyncObject = new object();

        public IDisposable ReadLock()
        {
            Monitor.Enter(this.SyncObject);

            return Disposables.Once(this.SyncObject, Monitor.Exit);
        }

        public IDisposable WriteLock()
        {
            Monitor.Enter(this.SyncObject);

            return Disposables.Once(this.SyncObject, Monitor.Exit);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}