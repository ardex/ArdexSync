using System.Threading;

namespace Ardex.Threading.Async
{
    public class SemaphoreAsyncLock : SemaphoreSlim, IAsyncLock
    {
        public SemaphoreAsyncLock() : base(1, 1)
        {

        }

        void IAsyncLock.Release()
        {
            this.Release();
        }
    }
}