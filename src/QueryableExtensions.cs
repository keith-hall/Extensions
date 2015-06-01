using System;
using System.Collections.Generic;
using System.Linq;

namespace HallLibrary.Extensions
{
	/// <summary>
	/// Contains static methods for working with queryables.
	/// </summary>
	public static class QueryableExtensions
	{
		#region Count
			/// <summary>
			/// Returns <c>true</c> if the number of elements in the <paramref name="queryable"/> exceeds the specified <paramref name="count"/>, querying only the minimum number of elements in the source necessary to determine the answer.
			/// </summary>
			/// <typeparam name="T">The type of elements in the queryable.</typeparam>
			/// <param name="queryable">The queryable source to check.</param>
			/// <param name="count">The count to exceed.</param>
			/// <returns>Returns <c>true</c> if the number of elements in the <paramref name="queryable"/> exceeds the specified <paramref name="count"/>.</returns>
			public static bool CountExceeds<T>(this IQueryable<T> queryable, int count)
			{
				return queryable.Take(count + 1).Count() > count;
			}
			
			/// <summary>
			/// Returns <c>true</c> if the number of elements in the <paramref name="queryable"/> equals the specified <paramref name="count"/>, querying only the minimum number of elements in the source necessary to determine the answer.
			/// </summary>
			/// <typeparam name="T">The type of elements in the queryable.</typeparam>
			/// <param name="queryable">The queryable source to check.</param>
			/// <param name="count">The count to match.</param>
			/// <returns>Returns <c>true</c> if the number of elements in the <paramref name="queryable"/> equals the specified <paramref name="count"/>.</returns>
			public static bool CountEquals<T>(this IQueryable<T> queryable, int count)
			{
				// note that this works because if there are more items than count, we just take one extra and compare it, which will return false
				// if there are less items, it will return false
				// and if there are count items, it can only take count, so the comparison will succeed
				return queryable.Take(count + 1).Count() == count;
			}
			
			
			/// <summary>
			/// Returns <c>true</c> if the number of elements in the <paramref name="queryable"/> is less than the specified <paramref name="count"/>, querying only the minimum number of elements in the source necessary to determine the answer.
			/// </summary>
			/// <typeparam name="T">The type of elements in the queryable.</typeparam>
			/// <param name="queryable">The queryable source to check.</param>
			/// <param name="count">The count to compare.</param>
			/// <returns>Returns <c>true</c> if the number of elements in the <paramref name="queryable"/> is less than the specified <paramref name="count"/>.</returns>
			public static bool CountIsLessThan<T>(this IEnumerable<T> queryable, int count)
			{
				return queryable.Take(count).Count() < count;
			}
		#endregion
	}
}
