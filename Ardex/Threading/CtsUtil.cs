using System.Threading;

namespace Ardex.Threading
{
	/// <summary>
	/// Utilities for working with CancellationTokenSources.
	/// </summary>
	public static class CtsUtil
    {
		/// <summary>
		/// Creates a new CancellationTokenSource instance,
		/// replaces the CancellationTokenSource at the given
		/// location with it, cancels the old instance if it was
		/// not a null reference and returns the new instance.
		/// </summary>
		public static CancellationTokenSource Renew(ref CancellationTokenSource location)
		{
			var newCts = new CancellationTokenSource();
			var oldCts = Interlocked.Exchange(ref location, newCts);

			if (oldCts != null)
			{
				oldCts.Cancel();
			}

			return newCts;
		}

		/// <summary>
		/// Replaces the CancellationTokenSource at the
		/// given location with a null reference and cancels
		/// the old instance if it was not a null reference.
		/// </summary>
		public static void Reset(ref CancellationTokenSource location)
		{
			var oldCts = Interlocked.Exchange(ref location, null);

			if (oldCts != null)
			{
				oldCts.Cancel();
			}
		}
    }
}

