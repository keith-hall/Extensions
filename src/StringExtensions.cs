using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HallLibrary.Extensions
{
	/// <summary>
	/// Contains extension methods for working with <see cref="System.String" /> objects
	/// </summary>
	public static class StringExtensions
	{
		#region IndexOf related methods
		/// <summary>
		/// Reports the zero-based index of the <i>end</i> of the first occurrence of the specified <paramref name="find" /> string in the current <see cref="System.String" /> object.
		/// </summary>
		/// <param name="value">The string to search in.</param>
		/// <param name="find">The string to seek.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <returns>Reports the zero-based index of the <i>end</i> of the first occurrence of the specified <paramref name="find" /> string in the current <see cref="System.String" /> object.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> is less than 0 (zero) or greater than the length of this string.</exception>
		public static int IndexOfEnd(this string value, string find, int? startIndex = null)
		{
			var pos = value.IndexOf(find, startIndex ?? 0, StringComparison.Ordinal);
			if (pos > -1)
				pos += find.Length;
			return pos;
		}

		/// <summary>
		/// Reports the zero-based indexes of all the occurrences of the specified <paramref name="find" /> string in the current <see cref="System.String" /> object.
		/// </summary>
		/// <param name="value">The string to search in.</param>
		/// <param name="find">The string to seek.</param>
		/// <returns>Reports the zero-based indexes of all the occurrences of the specified <paramref name="find" /> string in the current <see cref="System.String" /> object.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="find" /> is <c>null</c>.</exception>
		public static IEnumerable<int> AllIndexesOf(this string value, string find)
		{
			var pos = 0;
			while ((pos = value.IndexOf(find, pos, StringComparison.Ordinal)) > -1)
			{
				yield return pos;
				pos += find.Length;
			}
		}

		/// <summary>
		/// Reports the zero-based indexes of all the occurrences of the specified <paramref name="find" /> strings in the current <see cref="System.String" /> object.
		/// </summary>
		/// <param name="value">The string to search in.</param>
		/// <param name="find">The strings to seek.</param>
		/// <returns>Reports the zero-based indexes of all the occurrences of the specified <paramref name="find" /> strings in the current <see cref="System.String" /> object.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="find" /> contains a <c>null</c> value.</exception>
		/// <remarks>Searches through <paramref name="value" /> for each <paramref name="find" /> in parallel, for increased performance. Therefore, note that the order in which the indexes are returned is not deterministic. See <see cref="AllSortedIndexesOf" />.</remarks>
		private static IEnumerable<KeyValuePair<string, int>> AllIndexesOf(this string value, IEnumerable<string> find)
		{
			var results = new ConcurrentBag<KeyValuePair<string, int>>();
			Parallel.ForEach(find, search =>
								   {
									   var r = AllIndexesOf(value, search).Select(pos => new KeyValuePair<string, int>(search, pos));
									   foreach (var kvp in r) results.Add(kvp); // unable to yield return from here, so add results concurrently
								   });
			return results;
		}
		/* // alternative implementation that will keep the find indexes together
		{
			var results = new ConcurrentBag<List<KeyValuePair<string, int>>>();
			Parallel.ForEach(find, search => 
				results.Add(new List<KeyValuePair<string, int>>(
					AllIndexesOf(value, search)
					.Select(pos => new KeyValuePair<string, int>(search, pos))
				))
			);
			return results.SelectMany(e => e);
		}*/

		/// <summary>
		/// Reports the zero-based indexes of all the occurrences of the specified <paramref name="find" /> strings in the current <see cref="System.String" /> object, in the order in which they appear.
		/// </summary>
		/// <param name="value">The string to search in.</param>
		/// <param name="find">The strings to seek.</param>
		/// <returns>Reports the zero-based indexes of all the occurrences of the specified <paramref name="find" /> strings in the current <see cref="System.String" /> object, in the order in which they appear.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="find" /> contains a <c>null</c> value.</exception>
		public static IOrderedEnumerable<KeyValuePair<string, int>> AllSortedIndexesOf(this string value, IEnumerable<string> find)
		{
			var indexes = AllIndexesOf(value, find);
			return indexes.OrderBy(i => i.Value);
		}
		#endregion

		#region Text related methods
		/// <summary>
		/// Reports the number of times the specified <paramref name="find" /> string occurs in the current <see cref="System.String" /> object.
		/// </summary>
		/// <param name="value">The string to search in.</param>
		/// <param name="find">The string to seek.</param>
		/// <returns>Reports the number of times the specified <paramref name="find" /> string occurs in the current <see cref="System.String" /> object.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="find" /> is <c>null</c>.</exception>
		public static int CountOccurrences(this string value, string find)
		{
			return value.AllIndexesOf(find).Count();
		}

		/// <summary>
		/// Retrieves the text that occurs in the current <see cref="System.String" /> object before the first occurrence of <paramref name="find" />, or an empty string if there are no occurrences.
		/// </summary>
		/// <param name="value">The string to search in.</param>
		/// <param name="find">The string to seek.</param>
		/// <returns>Retrieves the text that occurs in the current <see cref="System.String" /> object before the first occurrence of <paramref name="find" />, or an empty string if there are no occurrences.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="find" /> is <c>null</c>.</exception>
		public static string TextBefore(this string value, string find)
		{
			var pos = value.IndexOf(find, StringComparison.Ordinal);
			if (pos == -1)
				pos = 0;
			return value.Substring(0, pos);
		}

		/// <summary>
		/// Retrieves the text that occurs in the current <see cref="System.String" /> object after the first occurrence of <paramref name="find" />, or an empty string if there are no occurrences.
		/// </summary>
		/// <param name="value">The string to search in.</param>
		/// <param name="find">The string to seek.</param>
		/// <returns>Retrieves the text that occurs in the current <see cref="System.String" /> object after the first occurrence of <paramref name="find" />, or an empty string if there are no occurrences.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="find" /> is <c>null</c>.</exception>
		public static string TextAfter(this string value, string find)
		{
			var pos = value.IndexOfEnd(find);
			if (pos == -1)
				pos = value.Length;
			return value.Substring(pos);
		}

		/// <summary>
		/// Retrieves the text that occurs in the current <see cref="System.String" /> object after the first occurrence of <paramref name="start" /> and before the first subsequent occurrence of <paramref name="end" />, or an empty string if either are not found.
		/// </summary>
		/// <param name="value">The string to search in.</param>
		/// <param name="start">The first string to seek.</param>
		/// <param name="end">The next string to seek.</param>
		/// <returns>Retrieves the text that occurs in the current <see cref="System.String" /> object after the first occurrence of <paramref name="start" /> and before the first subsequent occurrence of <paramref name="end" />, or an empty string if either are not found.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="start" /> or <paramref name="end" /> is <c>null</c>.</exception>
		public static string TextBetween(this string value, string start, string end)
		{
			return value.TextAfter(start).TextBefore(end);
		}

		/// <summary>
		/// Retrieves the text that occurs in the current <see cref="System.String" /> object after each occurrence of <paramref name="start" /> and before the first subsequent occurrence of <paramref name="end" />, or an empty enumerable.
		/// </summary>
		/// <param name="value">The string to search in.</param>
		/// <param name="start">The start string to seek.</param>
		/// <param name="end">The end string to seek.</param>
		/// <returns>Retrieves the text that occurs in the current <see cref="System.String" /> object after each occurrence of <paramref name="start" /> and before the first subsequent occurrence of <paramref name="end" />, or an empty enumerable.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="start" /> or <paramref name="end" /> is <c>null</c>.</exception>
		public static IEnumerable<string> AllTextBetween(this string value, string start, string end)
		{
			// get all indexes of the start and end tokens, sorted in order
			var results = value.AllSortedIndexesOf(new[] { start, end });
			var nextIsStart = true; // first we want a start token
			var startpos = -1;
			// for each occurrence
			foreach (var v in results)
			{
				// ignore the result if it isn't what we need next, or if it occurs before the previous token ends
				if (v.Key == (nextIsStart ? start : end) && v.Value >= startpos)
				{
					if (nextIsStart)
					{
						startpos = v.Value + start.Length;
						nextIsStart = false;
					}
					else
					{
						yield return value.Substring(startpos, v.Value - startpos);
						nextIsStart = true;
						startpos = v.Value + end.Length;
					}
				}
			}
		}
		#endregion
	}
}
