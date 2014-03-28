using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Ardex;

namespace Ardex.Diagnostics
{
    public static class PerfMonitor
    {
        public static IDisposable Time(string sessionName)
        {
            var sw = Stopwatch.StartNew();

            return Disposables.Once(() =>
                Debug.WriteLine("{0} completed in {1:0.###} seconds.", sessionName, (float)sw.ElapsedMilliseconds / 1000)
            );
        }

        public static IDisposable TimeWithAverage(string sessionName)
        {
            var sw = Stopwatch.StartNew();

            return Disposables.Once(() =>
            {
                var secondsTaken = (float)sw.ElapsedMilliseconds / 1000;
                var average = TimeWithAverageImplementation.CommitAndGetAverage(sessionName, secondsTaken);

                Debug.WriteLine("{0} completed in {1:0.###} seconds. Average duration: {2:0.###} seconds.", sessionName, secondsTaken, average);
            });
        }

        private static class TimeWithAverageImplementation
        {
            private static readonly Dictionary<string, List<float>> Sessions = new Dictionary<string, List<float>>();

            public static float CommitAndGetAverage(string sessionName, float timeTaken)
            {
                var list = default(List<float>);

                lock (Sessions)
                {
                    if (!Sessions.TryGetValue(sessionName, out list))
                    {
                        Sessions[sessionName] = list = new List<float>();
                    }
                }

                lock (list)
                {
                    list.Add(timeTaken);

                    return list.Average();
                }
            }
        }
    }

    public class PerfSession : IDisposable
    {
        private readonly Stopwatch Stopwatch;

        public string Name { get; private set; }

        public TimeSpan Duration
        {
            get { return this.Stopwatch.Elapsed; }
        }

        public PerfSession(string name)
        {
            this.Name = name;

            this.Stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            this.Stopwatch.Stop();
        }
    }
}