using System;

namespace Ardex.Sync
{
    /// <summary>
    /// Commonly used extensions on ISyncRepository{T}.
    /// </summary>
    public static class SyncRepositoryExtensions
    {
        /// <summary>
        /// Attempts to take out a read lock on the
        /// repository and throws a SyncDeadlockException
        /// if the lock cannot be obtained in the time
        /// period defined by SyncConstants.DeadlockTimeout.
        /// Returns an object which releases the lock when disposed.
        /// </summary>
        public static IDisposable ReadLock<T>(this ISyncRepository<T> repository)
        {
            if (!repository.Lock.TryEnterReadLock(SyncConstants.DeadlockTimeout))
            {
                throw new SyncDeadlockException();
            }

            return new DisposableActor(repository.Lock.ExitReadLock);
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
            if (!repository.Lock.TryEnterWriteLock(SyncConstants.DeadlockTimeout))
            {
                throw new SyncDeadlockException();
            }

            return new DisposableActor(repository.Lock.ExitWriteLock);
        }
    }
}
