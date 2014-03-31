using System;
using System.Threading;

using Ardex;

namespace Ardex.Sync.SyncLocks
{
    /// <summary>
    /// ISyncLock implementation which uses a
    /// ReaderWriterLockSlim under the covers.
    /// </summary>
    public sealed class ReaderWriterSyncLock : ISyncLock
    {
        /// <summary>
        /// Lock object specified when this instance was created.
        /// </summary>
        public ReaderWriterLockSlim Lock { get; private set; }

        /// <summary>
        /// True if this instance owns and manages
        /// (i.e. disposes of) the underlying lock object.
        /// </summary>
        public bool OwnsLock { get; private set; }

        /// <summary>
        /// Creates a new instance of ReaderWriterSyncLock.
        /// </summary>
        public ReaderWriterSyncLock()
            : this(new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion), true)
        {

        }

        /// <summary>
        /// Creates a new instance of ReaderWriterSyncLock.
        /// </summary>
        public ReaderWriterSyncLock(ReaderWriterLockSlim @lock, bool ownsLock)
        {
            this.Lock = @lock;
            this.OwnsLock = ownsLock;
        }

        /// <summary>
        /// Acquires a read lock and returns an
        /// object which releases it when disposed.
        /// </summary>
        public IDisposable ReadLock()
        {
            // Common case optimisation.
            if (this.Lock.IsReadLockHeld || this.Lock.IsWriteLockHeld)
        {
                return Disposables.Null;
            }

            if (!this.Lock.TryEnterReadLock(SyncConstants.DeadlockTimeout))
            {
                throw new SyncDeadlockException();
            }

            return Disposables.Once(this.Lock, l => l.ExitReadLock());
        }

        /// <summary>
        /// Acquires a write lock and returns an
        /// object which releases it when disposed.
        /// </summary>
        public IDisposable WriteLock()
        {
            // Common case optimisation.
            if (this.Lock.IsWriteLockHeld)
            {
                return Disposables.Null;
    }

            if (!this.Lock.TryEnterWriteLock(SyncConstants.DeadlockTimeout))
            {
                throw new SyncDeadlockException();
            }

            return Disposables.Once(this.Lock, l => l.ExitWriteLock());
        }

        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            if (this.OwnsLock)
            {
                this.Lock.Dispose();
            }
        }
    }
}