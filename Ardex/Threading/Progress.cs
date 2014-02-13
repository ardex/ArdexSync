using System;

namespace Ardex.Threading
{
    /// <summary>
    /// Facilitates operation progress calculation.
    /// </summary>
    public static class Progress
    {
        /// <summary>
        /// Calculates the progress percentage based
        /// on the given position and total figures.
        /// </summary>
        public static int Calculate(int position, int outOf)
        {
            var progressPercent = (int)((float)position / (float)outOf * 100f);
            
            return progressPercent;
        }

        /// <summary>
        /// Calculates the progress percentage based
        /// on the given position and total figures.
        /// </summary>
        public static int Calculate(long position, long outOf)
        {
			var progressPercent = (int)((double)position / (double)outOf * 100.0);
            
            return progressPercent;
        }
    }
}

