using System;
using System.Collections.Generic;
using System.Linq;

public static class EnumerableExtensions
{
	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
	{ // http://stackoverflow.com/questions/1287567/is-using-random-and-orderby-a-good-shuffle-algorithm
		T[] elements = source.ToArray();
		for (int i = elements.Length - 1; i >= 0; i--)
		{
			// Swap element "i" with a random earlier element it (or itself)
			// ... except we don't really need to swap it fully, as we can
			// return it immediately, and afterwards it's irrelevant.
			int swapIndex = rng.Next(i + 1);
			yield return elements[swapIndex];
			elements[swapIndex] = elements[i];
		}
	}

	public static bool CountExceeds<T>(this IEnumerable<T> enumerable, int count)
	{
		return enumerable.Take(count + 1).Count() > count;
	}
}
