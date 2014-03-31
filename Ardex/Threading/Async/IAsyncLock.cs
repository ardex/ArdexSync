using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ardex.Threading.Async
{
    public interface IAsyncLock : IDisposable
    {
        void Wait();
        Task WaitAsync();
        Task WaitAsync(CancellationToken ct);
        void Release();
    }
}