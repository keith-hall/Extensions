using System;
using System.Collections.Generic;
using System.Linq;

namespace HallLibrary.Extensions
{
	public static class StringExtensions
	{
		/// <summary>
		/// Reports the zero-based index of the <i>end</i> of the first occurrence of the specified string in the current <see cref="System.String" /> object.
		/// </summary>
		/// <param name="value">the string to search in</param>
		/// <param name="find">the string to seek</param>
		/// <param name="startIndex">the search starting position</param>
		/// <returns>Reports the zero-based index of the <i>end</i> of the first occurrence of the specified string in the current <see cref="System.String" /> object.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> is less than 0 (zero) or greater than the length of this string.</exception>
		public static int IndexOfEnd(this string value, string find, int? startIndex = null)
		{
			var pos = value.IndexOf(find, startIndex ?? 0, StringComparison.Ordinal);
			if (pos > -1)
				pos += find.Length;
			return pos;
		}

		/// <summary>
		/// Reports the zero-based indexes of all the occurrences of the specified strings in the current <see cref="System.String" /> object.
		/// </summary>
		/// <param name="value">the string to search in</param>
		/// <param name="find">the strings to seek</param>
		/// <returns>Reports the zero-based indexes of all the occurrences of the specified strings in the current <see cref="System.String" /> object.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="find" /> contains a <c>null</c> value.</exception>
		public static IEnumerable<KeyValuePair<string, int>> AllIndexesOf(this string value, IEnumerable<string> find)
		{
			foreach (var search in find)
			{
				var pos = 0;
				while ((pos = value.IndexOf(search, pos, StringComparison.Ordinal)) > -1)
				{
					yield return new KeyValuePair<string, int>(search, pos);
					pos += search.Length;
				}
			}
		}

		/// <summary>
		/// Reports the zero-based indexes of all the occurrences of the specified strings in the current <see cref="System.String" /> object, in the order in which they appear.
		/// </summary>
		/// <param name="value">the string to search in</param>
		/// <param name="find">the strings to seek</param>
		/// <returns>Reports the zero-based indexes of all the occurrences of the specified strings in the current <see cref="System.String" /> object, in the order in which they appear.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="find" /> contains a <c>null</c> value.</exception>
		public static IEnumerable<KeyValuePair<string, int>> AllSortedIndexesOf(this string value, IEnumerable<string> find)
		{
			var indexes = AllIndexesOf(value, find);
			return indexes.OrderBy(i => i.Value);
		}

		/// <summary>
		/// Reports the number of times the specified string occurs in the current <see cref="System.String" /> object.
		/// </summary>
		/// <param name="value">the string to search in</param>
		/// <param name="find">the string to seek</param>
		/// <returns>Reports the number of times the specified string occurs in the current <see cref="System.String" /> object.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="find" /> contains a <c>null</c> value.</exception>
		public static int CountOccurrences(this string value, IEnumerable<string> find)
		{
			return value.AllIndexesOf(find).Count();
		}

		/// <summary>
		/// Retrieves the text that occurs in the current <see cref="System.String" /> object before the first occurrence of <paramref name="find" />, or an empty string if there are no occurrences.
		/// </summary>
		/// <param name="value">the string to search in</param>
		/// <param name="find">the string to seek</param>
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
		/// <param name="value">the string to search in</param>
		/// <param name="find">the string to seek</param>
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
		/// <param name="value">the string to search in</param>
		/// <param name="start">the first string to seek</param>
		/// <param name="end">the next string to seek</param>
		/// <returns>Retrieves the text that occurs in the current <see cref="System.String" /> object after the first occurrence of <paramref name="start" /> and before the first subsequent occurrence of <paramref name="end" />, or an empty string if either are not found.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="start" /> or <paramref name="end" /> is <c>null</c>.</exception>
		public static string TextBetween(this string value, string start, string end)
		{
			return value.TextAfter(start).TextBefore(end);
		}

		/// <summary>
		/// Retrieves the text that occurs in the current <see cref="System.String" /> object after each occurrence of <paramref name="start" /> and before the first subsequent occurrence of <paramref name="end" />, or an empty enumerable.
		/// </summary>
		/// <param name="value">the string to search in</param>
		/// <param name="start">the start string to seek</param>
		/// <param name="end">the end string to seek</param>
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
	}
}
