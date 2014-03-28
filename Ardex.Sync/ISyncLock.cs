using System;

namespace Ardex.Sync
{
    /// <summary>
    /// Contract for Reader-Writer locks
    /// used by ISyncRepository{T}.
    /// </summary>
    public interface ISyncLock : IDisposable
    {
        /// <summary>
        /// Acquires a read lock and returns an
        /// object which releases it when disposed.
        /// </summary>
        IDisposable ReadLock();

        /// <summary>
        /// Acquires a write lock and returns an
        /// object which releases it when disposed.
        /// </summary>
        IDisposable WriteLock();
    }
}