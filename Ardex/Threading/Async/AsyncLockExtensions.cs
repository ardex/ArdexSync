using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ardex.Threading.Async
{
    public static class AsyncLockExtensions
    {
        public static IDisposable Lock(this IAsyncLock asyncLock)
        {
            asyncLock.Wait();

            return Disposables.Once(asyncLock, l => l.Release());
        }

        public static async Task<IDisposable> LockAsync(this IAsyncLock asyncLock)
        {
            await asyncLock.WaitAsync().ConfigureAwait(false);

            return Disposables.Once(asyncLock, l => l.Release());
        }

        public static async Task<IDisposable> LockAsync(this IAsyncLock asyncLock, CancellationToken ct)
        {
            await asyncLock.WaitAsync(ct).ConfigureAwait(false);

            return Disposables.Once(asyncLock, l => l.Release());
        }
    }
}