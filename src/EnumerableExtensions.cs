using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Data;

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
			if (source == null)
				throw new ArgumentNullException(nameof(source));
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
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (random == null)
				throw new ArgumentNullException(nameof(random));
			return ShuffleImpl(source, random);
		}
		
		private static IEnumerable<T> ShuffleImpl<T>(this IEnumerable<T> source, Random random)
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
			int countToCheck;
			if (enumerable is IQueryable<T>)
				countToCheck = ((IQueryable<T>)enumerable).Take(count + 1).Count();
			else if (enumerable is IList<T>)
				countToCheck = ((IList<T>)enumerable).Count;
			else
				countToCheck = enumerable.Take(count + 1).Count();
			return countToCheck > count;
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
			int countToCheck;
			if (enumerable is IQueryable<T>)
				countToCheck = ((IQueryable<T>)enumerable).Take(count + 1).Count();
			else if (enumerable is IList<T>)
				countToCheck = ((IList<T>)enumerable).Count;
			else
				countToCheck = enumerable.Take(count + 1).Count();
			return countToCheck == count;
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
			int countToCheck;
			if (enumerable is IQueryable<T>)
				countToCheck = ((IQueryable<T>)enumerable).Take(count).Count();
			else if (enumerable is IList<T>)
				countToCheck = ((IList<T>)enumerable).Count;
			else
				countToCheck = enumerable.Take(count).Count();
			return countToCheck < count;
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
		
		/// <summary>
		/// Sorts the specified sequence of <paramref name="items" /> in a natural order.
		/// </summary>
		/// <param name="items">The sequence of items to sort.</param>
		/// <param name="selector">The function to extract the key to use when sorting the sequence.</param>
		/// <param name="stringComparer">The <see cref="StringComparer" /> to use when comparing values for sorting.</param>
		/// <returns>A naturally sorted sequence of <paramref name="items" />.</returns>
		public static IEnumerable<T> OrderByNatural<T>(this IEnumerable<T> items, Func<T, string> selector, StringComparer stringComparer = null)
		{ // modified from http://stackoverflow.com/a/11720793/4473405
			var regex = new Regex(@"\d+", RegexOptions.Compiled);
			
			int maxDigits = items
				.SelectMany(i => regex.Matches(selector(i)).OfType<Match>().Select(digitChunk => (int?)digitChunk.Value.Length))
				.Max() ?? 0;
			
			return items.OrderBy(i => regex.Replace(selector(i), match => match.Value.PadLeft(maxDigits, '0')), stringComparer ?? StringComparer.CurrentCulture);
		}
		
		/// <summary>
		/// Convert the specified sequence of <paramref name="items" /> to a <see cref="DataTable" />.
		/// </summary>
		/// <typeparam name="T">The type of the elements in the sequence of <paramref name="items" />.</typeparam>
		/// <param name="items">The sequence of items to convert.</param>
		/// <returns>A <see cref="DataTable" /> populated with <paramref name="items" />, where the properties and fields are used as column headings.</returns>
		public static DataTable ToDataTable<T>(this IEnumerable<T> items) where T : class
		{
			var table = new DataTable(typeof(T).Name);
			var props = typeof(T).GetProperties().Where(p => !p.GetIndexParameters().Any() && !p.GetCustomAttributes(false).Any(a => a is System.Data.Linq.Mapping.AssociationAttribute));
			var fields = typeof(T).GetFields();
			var combined = props.Select(p => Tuple.Create<Func<object, object>, string, Type>(p.GetValue, p.Name, p.PropertyType)).Concat(fields.Select(f => Tuple.Create<Func<object, object>, string, Type>(f.GetValue, f.Name, f.FieldType))).ToArray();
			// add the properties and fields as columns to the datatable
			foreach (var prop in combined)
			{
				Type propType = prop.Item3;

				// if it is a nullable type, get the underlying type 
				if (propType.IsGenericType && propType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
					propType = Nullable.GetUnderlyingType(propType);

				table.Columns.Add(prop.Item2, propType);
			}
			
			table.BeginLoadData();
			// for each item, add the property and field values as a row to the datatable
			foreach (var item in items)
			{
				var values = combined.Select(p => p.Item1(item));
				table.Rows.Add(values.ToArray());
			}
			table.EndLoadData();

			return table;
		}
		
		// https://stackoverflow.com/a/489421/4473405
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			var seenKeys = new HashSet<TKey>();
			foreach (var element in source)
			{
				if (seenKeys.Add(keySelector(element)))
				{
					yield return element;
				}
			}
		}
		
		public static IEnumerable<T> ConsecutiveDistinct<T>(this IEnumerable<T> input) {
			return ConsecutiveDistinct(input, v => v);
		}
		
		// For completeness, this is two methods to ensure that the null check 
		// is done eagerly while the loop is done lazily
		public static IEnumerable<T> ConsecutiveDistinct<T, TKey>(this IEnumerable<T> input, Func<T, TKey> keySelector) {
			if (input == null)
				throw new ArgumentNullException(nameof(input));
			return ConsecutiveDistinctImpl(input, keySelector);
		}
		
		// modified from http://stackoverflow.com/a/5729893/4473405
		private static IEnumerable<T> ConsecutiveDistinctImpl<T, TKey>(this IEnumerable<T> input, Func<T, TKey> keySelector) {
			bool isFirst = true;
			TKey last = default(TKey);
			foreach (var item in input) {
				var key = keySelector(item);
				if (isFirst || !object.Equals(key, last)) {
					yield return item;
					last = key;
					isFirst = false;
				}
			}
		}
		
		/// <summary>
		/// Ranks the specified sequence of <paramref name="groupedItems" />, flattening the groupings.
		/// </summary>
		/// <param name="groupedItems">The sequence of items to sort.</param>
		/// <param name="dense">Whether or not to leave gaps in the ranking.</param>
		/// <param name="projection">The projection to apply to the items in the sequence combined with the rank.</param>
		/// <returns>The flattened sequence of <paramref name="groupedItems" /> with a rank for each element in the sequence.</returns>
		/// <remarks>If desired, the results can be re-grouped again using GroupBy.</remarks>
		public static IEnumerable<TResult> Rank<TKey, TValue, TResult> (this IEnumerable<IGrouping<TKey, TValue>> groupedItems, bool dense, Func<TKey, TValue, int, TResult> projection) {
			if (groupedItems == null)
				throw new ArgumentNullException(nameof(groupedItems));
			if (projection == null)
				throw new ArgumentNullException(nameof(projection));
			return RankImpl(groupedItems, dense, projection);
		}
		
		private static IEnumerable<TResult> RankImpl<TKey, TValue, TResult> (this IEnumerable<IGrouping<TKey, TValue>> groupedItems, bool dense, Func<TKey, TValue, int, TResult> projection) {
			var rank = 1;
			foreach (var grp in groupedItems)
			{
				var groupRank = rank;
				foreach (var item in grp)
				{
					yield return projection(grp.Key, item, groupRank);
					if (!dense)
						rank++;
				}
				if (dense)
					rank++;
			}
		}
		
		/*// NOTE: disabled due to being able to use GroupBy on the flattened Rank sequence
		/// <summary>
		/// Ranks the specified sequence of <paramref name="groupedItems" />, keeping the group.
		/// </summary>
		/// <param name="groupedItems">The sequence of items to sort.</param>
		/// <returns>The sequence of <paramref name="groupedItems" /> with a rank for each element in the sequence.</returns>
		public static IEnumerable<Tuple<IGrouping<TKey, TValue>, int>> RankGroup<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> groupedItems)
		{
			return RankGroup(groupedItems, false); // default to false for density because if desired, can just use a Select overload with index
		}
	
		private static IEnumerable<Tuple<IGrouping<TKey, TValue>, int>> RankGroup<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> groupedItems, bool dense)
		{
			var rank = 1;
			foreach (var grp in groupedItems)
			{
				yield return Tuple.Create(grp, rank);
				rank += dense ? 1 : grp.Count();
			}
		}*/
		
		/// <summary>
		/// Returns the current value of an enumerable with the previous value.
		/// </summary>
		/// <typeparam name="TSource">The source type.</typeparam>
		/// <typeparam name="TOutput">The output type after the <paramref name="projection" /> has been applied.</typeparam>
		/// <param name="source">The enumerable.</param>
		/// <param name="projection">The projection to apply.</param>
		/// <returns>The current enumerable value with the previous value.</returns>
		public static IEnumerable<TOutput> WithPrevious<TSource, TOutput>(this IEnumerable<TSource> source, Func<TSource, TSource, TOutput> projection)
		{ // http://www.zerobugbuild.com/?p=213
			var prev = default(TSource);
			foreach (var item in source)
			{
				yield return projection(prev, item);
				prev = item;
			}
		}
	}
}
