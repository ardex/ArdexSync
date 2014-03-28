using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ardex.Sync.SyncLocks
{
    public class DiagnosticSyncLock : ISyncLock
    {
        #if DEBUG
        private static readonly List<float> ReadLockDurations = new List<float>();
        private static readonly List<float> WriteLockDurations = new List<float>();
        #endif

        private readonly ISyncLock InnerLock;

        public DiagnosticSyncLock(ISyncLock syncLock)
        {
            this.InnerLock = syncLock;
        }

        public IDisposable ReadLock(/*[CallerMemberName] string caller = null*/)
        {
            #if DEBUG

            var sw = Stopwatch.StartNew();
            var unlocker = this.InnerLock.ReadLock();

            Debug.WriteLine(
                "{0}.ReadLock() acquisition took {1:0.###} seconds. Caller: {2}",
                this.InnerLock.GetType().Name,
                (float)sw.ElapsedMilliseconds / 1000,
                "UNKNOWN"
            );

            return Disposables.Once(() =>
            {
                unlocker.Dispose();

                var lockDuration = (float)sw.ElapsedMilliseconds / 1000;

                Debug.WriteLine(
                    "{0}.ReadLock() released after being held for {1:0.###} seconds. Caller: {2}",
                    this.InnerLock.GetType().Name,
                    lockDuration,
                    "UNKNOWN"
                );

                lock (ReadLockDurations)
                {
                    ReadLockDurations.Add(lockDuration);
                    Debug.WriteLine("Average read lock duration: {0:0.###} seconds.", ReadLockDurations.Average());
                }
            });

            #else

            return this.InnerLock.ReadLock();

            #endif
        }

        public IDisposable WriteLock(/*[CallerMemberName] string caller = null*/)
        {
            #if DEBUG

            var sw = Stopwatch.StartNew();
            var unlocker = this.InnerLock.WriteLock();

            Debug.WriteLine(
                "{0}.WriteLock() acquisition took {1:0.###} seconds. Caller: {2}",
                this.InnerLock.GetType().Name,
                (float)sw.ElapsedMilliseconds / 1000,
                "UNKNOWN"
            );

            return Disposables.Once(() =>
            {
                unlocker.Dispose();

                var lockDuration = (float)sw.ElapsedMilliseconds / 1000;

                Debug.WriteLine(
                    "{0}.WriteLock() released after being held for {1:0.###} seconds. Caller: {2}",
                    this.InnerLock.GetType().Name,
                    lockDuration,
                    "UNKNOWN"
                );

                lock (WriteLockDurations)
                {
                    WriteLockDurations.Add(lockDuration);
                    Debug.WriteLine("Average write lock duration: {0:0.###} seconds.", WriteLockDurations.Average());
                }
            });

            #else

            return this.InnerLock.WriteLock();

            #endif
        }

        public void Dispose()
        {
            
        }
    }
}
