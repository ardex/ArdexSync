using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ardex.Util
{
    /// <summary>
    /// Provides a convenient way to time operations.
    /// </summary>
	public static class DelegateTimer
	{
        /// <summary>
        /// Executes the given action and returns the time it took.
        /// </summary>
		public static TimeSpan Time(Action action)
		{
			var stopwatch = Stopwatch.StartNew();
			
            try
            {
			action();
            }
            finally
            {
			stopwatch.Stop();
            }
				
			return stopwatch.Elapsed;
		}
	}
}

