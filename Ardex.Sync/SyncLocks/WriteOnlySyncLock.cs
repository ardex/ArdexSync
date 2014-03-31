using System;
using System.Threading;

namespace Ardex.Sync.SyncLocks
{
    public class WriteOnlySyncLock : ISyncLock
    {
        private readonly object Lock = new object();

        public IDisposable ReadLock()
        {
            return Disposables.Null;
        }

        public IDisposable WriteLock()
        {
            Monitor.Enter(this.Lock);

            return Disposables.Once(this.Lock, Monitor.Exit);
        }

        public void Dispose()
        {

        }
    }
}