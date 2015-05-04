using System;
using System.Collections.Generic;
using System.Linq;

namespace HallLibrary.Extensions
{
	/// <summary>
	/// Contains static methods for working with enumerables.
	/// </summary>
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Randomise the order of the elements in the <paramref name="source"/> enumerable.
		/// </summary>
		/// <typeparam name="T">The type of the elements in the <paramref name="source"/> enumerable.</typeparam>
		/// <param name="source">The enumerable containing the elements to shuffle.</param>
		/// <returns>An enumerable containing the same elements but in a random order.</returns>
		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
		{
			return Shuffle(source, new Random());
		}

		/// <summary>
		/// Randomise the order of the elements in the <paramref name="source"/> enumerable, using the specified <paramref name="random"/> seed.
		/// </summary>
		/// <typeparam name="T">The type of the elements in the <paramref name="source"/> enumerable.</typeparam>
		/// <param name="source">The enumerable containing the elements to shuffle.</param>
		/// <param name="random">The random seed to use.</param>
		/// <returns>An enumerable containing the same elements but in a random order.</returns>
		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random random)
		{ // http://stackoverflow.com/questions/1287567/is-using-random-and-orderby-a-good-shuffle-algorithm
			var elements = source.ToArray();
			for (var i = elements.Length - 1; i >= 0; i--)
			{
				// Swap element "i" with a random earlier element it (or itself)
				// ... except we don't really need to swap it fully, as we can
				// return it immediately, and afterwards it's irrelevant.
				var swapIndex = random.Next(i + 1); // Random.Next maxValue is an exclusive upper-bound, which is why we add 1
				yield return elements[swapIndex];
				elements[swapIndex] = elements[i];
			}
		}

		/// <summary>
		/// Returns <c>true</c> if the number of elements in the <paramref name="enumerable"/> exceeds the specified <paramref name="count"/>, without enumerating through every element.
		/// </summary>
		/// <typeparam name="T">The type of elements in the enumerable.</typeparam>
		/// <param name="enumerable">The enumerable sequence to check.</param>
		/// <param name="count">The count to exceed.</param>
		/// <returns>Returns <c>true</c> if the number of elements in the <paramref name="enumerable"/> exceeds the specified <paramref name="count"/>.</returns>
		public static bool CountExceeds<T>(this IEnumerable<T> enumerable, int count)
		{
			return enumerable.Take(count + 1).Count() > count;
		}

		/// <summary>
		/// Converts the current <paramref name="value"/> to an enumerable, containing the <paramref name="value"/> as it's sole element.
		/// </summary>
		/// <typeparam name="T">The type of the element.</typeparam>
		/// <param name="value">The value that the new enumerable will contain.</param>
		/// <returns>Returns an enumerable containing a single element.</returns>
		public static IEnumerable<T> AsSingleEnumerable<T>(this T value)
		{
			//x return Enumerable.Repeat(value, 1);
			return new[] { value };
		}
		
		/// <summary>
		/// Concatenates a sequence with a single value of the same type.
		/// </summary>
		/// <param name="first">The first sequence to concatenate.</param>
		/// <param name="second">The value to concatenate to the end of the <paramref name="first" /> sequence.</param>
		/// <returns>The <paramref name="first" /> sequence concatenated with <paramref name="second" />.</returns>
		public static IEnumerable<T> Concat<T> (this IEnumerable<T> first, T second) {
			return first.Concat(second.AsSingleEnumerable());
		}
	}
}
