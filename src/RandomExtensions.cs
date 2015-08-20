using System;

namespace HallLibrary.Extensions
{
	public static class RandomExtensionMethods
	{
		/// <summary>
		/// Returns a random long from min (inclusive) to max (exclusive).
		/// </summary>
		/// <param name="random">The given random instance.</param>
		/// <param name="min">The inclusive minimum bound.</param>
		/// <param name="max">The exclusive maximum bound.  Must be greater than <paramref name="min" />.</param>
		/// <remarks>Code taken from http://stackoverflow.com/a/13095144/4473405</remarks>
		public static long NextLong(this Random random, long min, long max)
		{
			if (max <= min)
				throw new ArgumentOutOfRangeException(nameof(max), string.Format("{0} must be > {1}!", nameof(max), nameof(min)));
			
			// Working with ulong so that modulo works correctly with values > long.MaxValue
			ulong uRange = (ulong)(max - min);
			
			// Prevent a modolo bias; see http://stackoverflow.com/a/10984975/238419
			// for more information.
			// In the worst case, the expected number of calls is 2 (though usually it's
			// much closer to 1) so this loop doesn't really hurt performance at all.
			ulong ulongRand;
			do
			{
				byte[] buf = new byte[8];
				random.NextBytes(buf);
				ulongRand = (ulong)BitConverter.ToInt64(buf, 0);
			} while (ulongRand > ulong.MaxValue - ((ulong.MaxValue % uRange) + 1) % uRange);
			
			return (long)(ulongRand % uRange) + min;
		}
	}
}
