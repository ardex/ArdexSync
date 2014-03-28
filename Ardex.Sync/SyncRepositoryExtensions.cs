//using System;

//#if DEBUG

//using System.Collections.Generic;
//    using System.Diagnostics;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Threading;
//using System.Threading.Tasks;

//#endif

//namespace Ardex.Sync
//{
//    /// <summary>
//    /// Commonly used extensions on ISyncRepository{T}.
//    /// </summary>
//    public static class SyncRepositoryExtensions
//    {
//        #if DEBUG
//        private static readonly List<float> ReadLockDurations = new List<float>();
//        private static readonly List<float> WriteLockDurations = new List<float>();
//        #endif

//        #if DEBUG

//        /// <summary>
//        /// Attempts to take out a read lock on the
//        /// repository and throws a SyncDeadlockException
//        /// if the lock cannot be obtained in the time
//        /// period defined by SyncConstants.DeadlockTimeout.
//        /// Returns an object which releases the lock when disposed.
//        /// </summary>
//        public static IDisposable ReadLock<T>(this ISyncRepository<T> repository, [CallerMemberName] string caller = null)
//        {
//            var sw = Stopwatch.StartNew();

//            try
//            {
//                if (repository.SyncLock.IsReadLockHeld || repository.SyncLock.IsWriteLockHeld)
//                {
//                    return Disposables.Null;
//                }

//                if (repository.SyncLock.TryEnterReadLock(SyncConstants.DeadlockTimeout))
//                {
//                    return Disposables.Once(() =>
//                    {
//                        repository.SyncLock.ExitReadLock();

//                        var lockDuration = (float)sw.ElapsedMilliseconds / 1000;

//                        Debug.WriteLine(
//                            "ISyncRepository<{0}>.ReadLock() released after being held for {1:0.###} seconds. Caller: {2}",
//                            typeof(T).Name,
//                            lockDuration,
//                            caller
//                        );

//                        lock (ReadLockDurations)
//        {
//                            ReadLockDurations.Add(lockDuration);
//                            Debug.WriteLine("Average read lock duration: {0:0.###} seconds.", ReadLockDurations.Average());
//                        }
//                    });
//                }

//                throw new SyncDeadlockException();
//            }
//            finally
//            {
//                Debug.WriteLine(
//                    "ISyncRepository<{0}>.ReadLock() acquisition took {1:0.###} seconds. Caller: {2}",
//                    typeof(T).Name,
//                    (float)sw.ElapsedMilliseconds / 1000,
//                    caller
//                );
//            }
//        }

//        /// <summary>
//        /// Attempts to take out a write lock on the
//        /// repository and throws a SyncDeadlockException
//        /// if the lock cannot be obtained in the time
//        /// period defined by SyncConstants.DeadlockTimeout.
//        /// Returns an object which releases the lock when disposed.
//        /// </summary>
//        public static IDisposable WriteLock<T>(this ISyncRepository<T> repository, [CallerMemberName] string caller = null)
//        {
//            var sw = Stopwatch.StartNew();

//            try
//            {
//                if (repository.SyncLock.IsWriteLockHeld)
//                {
//                    return Disposables.Null;
//                }

//                if (repository.SyncLock.TryEnterWriteLock(SyncConstants.DeadlockTimeout))
//            {
//                    return Disposables.Once(() =>
//                {
//                        repository.SyncLock.ExitWriteLock();

//                        var lockDuration = (float)sw.ElapsedMilliseconds / 1000;

//                        Debug.WriteLine(
//                            "ISyncRepository<{0}>.WriteLock() released after being held for {1:0.###} seconds. Caller: {2}",
//                            typeof(T).Name,
//                            lockDuration,
//                            caller
//                        );

//                        lock (WriteLockDurations)
//                        {
//                            WriteLockDurations.Add(lockDuration);
//                            Debug.WriteLine("Average write lock duration: {0:0.###} seconds.", WriteLockDurations.Average());
//                }
//                    });
//            }

//                throw new SyncDeadlockException();
//            }
//            finally
//            {
//                Debug.WriteLine(
//                    "ISyncRepository<{0}>.WriteLock() acquisition took {1:0.###} seconds. Caller: {2}",
//                    typeof(T).Name,
//                    (float)sw.ElapsedMilliseconds / 1000,
//                    caller
//                );
//            }
//        }

//        public static async void ReaderWriterLockTest(int degreeOfParallelisation, Action sleepAction)
//        {
//            using (var syncLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion))
//            {
//                var readStopwatch = Stopwatch.StartNew();

//                var readTasks = Enumerable
//                    .Range(0, degreeOfParallelisation)
//                    .Select(_ => Task.Run(() =>
//                    {
//                        syncLock.EnterReadLock();
//                        sleepAction();
//                        syncLock.ExitReadLock();
//                    }))
//                    .ToArray();

//                await Task.WhenAll(readTasks);

//                Debug.WriteLine("ReadTasks completed in {0:0.###} seconds.", (float)readStopwatch.ElapsedMilliseconds / 1000);

//                var writeStopwatch = Stopwatch.StartNew();

//                var writeTasks = Enumerable
//                    .Range(0, degreeOfParallelisation)
//                    .Select(_ => Task.Run(() =>
//                    {
//                        syncLock.EnterWriteLock();
//                        sleepAction();
//                        syncLock.ExitWriteLock();
//                    }))
//                    .ToArray();

//                await Task.WhenAll(writeTasks);

//                Debug.WriteLine("WriteTasks completed in {0:0.###} seconds.", (float)writeStopwatch.ElapsedMilliseconds / 1000);
//            }
//        }

//        #else

//        /// <summary>
//        /// Attempts to take out a read lock on the
//        /// repository and throws a SyncDeadlockException
//        /// if the lock cannot be obtained in the time
//        /// period defined by SyncConstants.DeadlockTimeout.
//        /// Returns an object which releases the lock when disposed.
//        /// </summary>
//        public static IDisposable ReadLock<T>(this ISyncRepository<T> repository)
//        {
//            if (repository.SyncLock.IsReadLockHeld || repository.SyncLock.IsWriteLockHeld)
//            {
//                return Disposables.Null;
//            }

//            if (repository.SyncLock.TryEnterReadLock(SyncConstants.DeadlockTimeout))
//            {
//                return new DisposableActor(repository.SyncLock.ExitReadLock);
//            }

//            throw new SyncDeadlockException();
//        }

//        /// <summary>
//        /// Attempts to take out a write lock on the
//        /// repository and throws a SyncDeadlockException
//        /// if the lock cannot be obtained in the time
//        /// period defined by SyncConstants.DeadlockTimeout.
//        /// Returns an object which releases the lock when disposed.
//        /// </summary>
//        public static IDisposable WriteLock<T>(this ISyncRepository<T> repository)
//        {
//            if (repository.SyncLock.IsWriteLockHeld)
//                {
//                return Disposables.Null;
//            }

//            if (repository.SyncLock.TryEnterWriteLock(SyncConstants.DeadlockTimeout))
//            {
//                return new DisposableActor(repository.SyncLock.ExitWriteLock);
//            }

//            throw new SyncDeadlockException();
//        }

//        #endif
//    }
//}