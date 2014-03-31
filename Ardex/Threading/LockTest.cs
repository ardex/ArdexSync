using System.Diagnostics;
using System.Threading.Tasks;

namespace Ardex.Threading
{
    public static class LockTest
    {
        /// <summary>
        /// Simple demonstration of the lock
        /// statement failure in Xamarin.iOS.
        /// </summary>
        public static async Task RunAsync()
        {
            const int numWorkers = 4;
            const int numOperationsPerWorker = 10000;

            // This is the value which will be incremented.
            var value = 0;
            var valueLock = new object();

            for (var i = 0; i < numOperationsPerWorker; i++)
            {
                // Run X workers in parallel.
                var workers = new Task[numWorkers];

                for (var w = 0; w < numWorkers; w++)
                {
                    workers[w] = Task.Run(() =>
                    {
                        lock (valueLock)
                        {
                            // Increment the value in a non-atomic way.
                            // If the lock fails, this will not 
                            // consistently produce the expected result.
                            value++;
                        }
                    });
                }

                await Task.WhenAll(workers).ConfigureAwait(false);
            }

            Debug.WriteLine("Value: {0}.", value);

            var expectedValue = numOperationsPerWorker * numWorkers;

            Debug.Assert(
                value == expectedValue,
                string.Format("Lock statement failed. Expected value: {0}. Actual value: {1}.", expectedValue, value)
            );
        }
    }
}