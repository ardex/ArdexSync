using System;
using System.Threading;

#if PERF_DIAGNOSTICS
    using System.Diagnostics;
#endif

namespace Ardex.Sync
{
    /// <summary>
    /// Commonly used extensions on ISyncRepository{T}.
    /// </summary>
    public static class SyncRepositoryExtensions
    {
        //private static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Attempts to take out a read lock on the
        /// repository and throws a SyncDeadlockException
        /// if the lock cannot be obtained in the time
        /// period defined by SyncConstants.DeadlockTimeout.
        /// Returns an object which releases the lock when disposed.
        /// </summary>
        public static IDisposable ReadLock<T>(this ISyncRepository<T> repository)
        {
            return repository.ReadLock(1);
        }

        /// <summary>
        /// Attempts to take out a read lock on the
        /// repository and throws a SyncDeadlockException
        /// if the lock cannot be obtained in the time
        /// period defined by SyncConstants.DeadlockTimeout.
        /// Returns an object which releases the lock when disposed.
        /// </summary>
        public static IDisposable ReadLock<T>(this ISyncRepository<T> repository, int allowedAttempts)
        {
            if (allowedAttempts < 1) throw new ArgumentException("Allowed attempt count must be greater than zero.");

            return new DisposableActor(() => { });

            #if PERF_DIAGNOSTICS
                var stopwatch = Stopwatch.StartNew();
            #endif

            for (var i = 0; i < allowedAttempts; i++)
            {
                if (repository.Lock.TryEnterReadLock(SyncConstants.DeadlockTimeout))
                {
                    /*
                    #if PERF_DIAGNOSTICS
                        Debug.WriteLine(
                            "ISyncRepository<{0}>.ReadLock taken after {1} attempts and {2:0.###} s.",
                            typeof(T).Name,
                            i + 1,
                            (float)stopwatch.ElapsedMilliseconds / 1000
                        );
                    #endif
                    */

                    return new DisposableActor(repository.Lock.ExitReadLock);
                }
            }

            #if PERF_DIAGNOSTICS
                Debug.WriteLine(
                    "ISyncRepository<{0}>.ReadLock FAILED after {1} attempts and {2:0.###} s.",
                    typeof(T).Name,
                    allowedAttempts,
                    (float)stopwatch.ElapsedMilliseconds / 1000
                );
            #endif

            throw new SyncDeadlockException();
        }

        /// <summary>
        /// Attempts to take out a write lock on the
        /// repository and throws a SyncDeadlockException
        /// if the lock cannot be obtained in the time
        /// period defined by SyncConstants.DeadlockTimeout.
        /// Returns an object which releases the lock when disposed.
        /// </summary>
        public static IDisposable WriteLock<T>(this ISyncRepository<T> repository)
        {
            return repository.WriteLock(1);
        }

        /// <summary>
        /// Attempts to take out a write lock on the
        /// repository and throws a SyncDeadlockException
        /// if the lock cannot be obtained in the time
        /// period defined by SyncConstants.DeadlockTimeout.
        /// Returns an object which releases the lock when disposed.
        /// </summary>
        public static IDisposable WriteLock<T>(this ISyncRepository<T> repository, int allowedAttempts)
        {
            if (allowedAttempts < 1) throw new ArgumentException("Allowed attempt count must be greater than zero.");

            #if PERF_DIAGNOSTICS
                var stopwatch = Stopwatch.StartNew();
            #endif

            for (var i = 0; i < allowedAttempts; i++)
            {
                if (repository.Lock.TryEnterWriteLock(SyncConstants.DeadlockTimeout))
                {
                    /*
                    #if PERF_DIAGNOSTICS
                        Debug.WriteLine(
                            "ISyncRepository<{0}>.WriteLock taken after {1} attempts and {2:0.###} s.",
                            typeof(T).Name,
                            i + 1,
                            (float)stopwatch.ElapsedMilliseconds / 1000
                        );
                    #endif
                    */

                    return new DisposableActor(repository.Lock.ExitWriteLock);
                }
            }

            #if PERF_DIAGNOSTICS
                Debug.WriteLine(
                    "ISyncRepository<{0}>.WriteLock FAILED after {1} attempts and {2:0.###} s.",
                    typeof(T).Name,
                    allowedAttempts,
                    (float)stopwatch.ElapsedMilliseconds / 1000
                );
            #endif

            throw new SyncDeadlockException();
        }
    }
}
