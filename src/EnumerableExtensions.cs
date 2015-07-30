using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HallLibrary.Extensions
{
	/// <summary>
	/// Contains static methods for working with enumerables.
	/// </summary>
	public static class EnumerableExtensions
	{
		#region Shuffle
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
		#endregion
		
		#region Count
		/// <summary>
		/// Returns <c>true</c> if the number of elements in the <paramref name="enumerable"/> exceeds the specified <paramref name="count"/>, iterating through only the minimum number of elements in the sequence necessary to determine the answer.
		/// </summary>
		/// <typeparam name="T">The type of elements in the enumerable.</typeparam>
		/// <param name="enumerable">The enumerable sequence to check.</param>
		/// <param name="count">The count to exceed.</param>
		/// <returns>Returns <c>true</c> if the number of elements in the <paramref name="enumerable"/> exceeds the specified <paramref name="count"/>.</returns>
		public static bool CountExceeds<T>(this IEnumerable<T> enumerable, int count)
		{
			if (enumerable is IQueryable<T>)
				return ((IQueryable<T>)enumerable).Take(count + 1).Count() > count;
			else
				return enumerable.Take(count + 1).Count() > count;
		}
		
		/// <summary>
		/// Returns <c>true</c> if the number of elements in the <paramref name="enumerable"/> equals the specified <paramref name="count"/>, iterating through only the minimum number of elements in the sequence necessary to determine the answer.
		/// </summary>
		/// <typeparam name="T">The type of elements in the enumerable.</typeparam>
		/// <param name="enumerable">The enumerable sequence to check.</param>
		/// <param name="count">The count to match.</param>
		/// <returns>Returns <c>true</c> if the number of elements in the <paramref name="enumerable"/> equals the specified <paramref name="count"/>.</returns>
		public static bool CountEquals<T>(this IEnumerable<T> enumerable, int count)
		{
			// note that this works because if there are more items than count, we just take one extra and compare it, which will return false
			// if there are less items, it will return false
			// and if there are count items, it can only take count, so the comparison will succeed
			if (enumerable is IQueryable<T>)
				return ((IQueryable<T>)enumerable).Take(count + 1).Count() == count;
			else
				return enumerable.Take(count + 1).Count() == count;
		}
		
		
		/// <summary>
		/// Returns <c>true</c> if the number of elements in the <paramref name="enumerable"/> is less than the specified <paramref name="count"/>, iterating through only the minimum number of elements in the sequence necessary to determine the answer.
		/// </summary>
		/// <typeparam name="T">The type of elements in the enumerable.</typeparam>
		/// <param name="enumerable">The enumerable sequence to check.</param>
		/// <param name="count">The count to compare.</param>
		/// <returns>Returns <c>true</c> if the number of elements in the <paramref name="enumerable"/> is less than the specified <paramref name="count"/>.</returns>
		public static bool CountIsLessThan<T>(this IEnumerable<T> enumerable, int count)
		{
			if (enumerable is IQueryable<T>)
				return ((IQueryable<T>)enumerable).Take(count).Count() < count;
			else
				return enumerable.Take(count).Count() < count;
		}
		#endregion

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
		public static IEnumerable<T> Concat<T> (this IEnumerable<T> first, T second)
		{
			return first.Concat(second.AsSingleEnumerable());
		}
		
		public static IEnumerable<T> OrderByNatural<T>(this IEnumerable<T> items, Func<T, string> selector, StringComparer stringComparer = null)
		{
			var regex = new Regex(@"\d+", RegexOptions.Compiled);
			
			int maxDigits = items
				.SelectMany(i => regex.Matches(selector(i)).OfType<Match>().Select(digitChunk => (int?)digitChunk.Value.Length))
				.Max() ?? 0;
			
			return items.OrderBy(i => regex.Replace(selector(i), match => match.Value.PadLeft(maxDigits, '0')), stringComparer ?? StringComparer.CurrentCulture);
		}
	}
}
