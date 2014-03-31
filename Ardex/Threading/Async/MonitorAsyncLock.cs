using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ardex.Threading.Async
{
    public class MonitorAsyncLock : IAsyncLock
    {
        private readonly object Lock = new object();

        public void Wait()
        {
            Monitor.Enter(this.Lock);
        }

        public Task WaitAsync()
        {
            throw new NotImplementedException();
        }

        public Task WaitAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public void Release()
        {
            Monitor.Exit(this.Lock);
        }

        public void Dispose()
        {

        }
    }
}