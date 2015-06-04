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
		/// <summary>
		/// Returns a single value from the specified <paramref name="queryable"/>, or throws an Exception if it contains multiple values.
		/// </summary>
		/// <typeparam name="T">The type of elements in the queryable.</typeparam>
		/// <param name="queryable">The queryable sequence to get the single value of.</param>
		/// <returns>Returns the single value from the specified <paramref name="queryable"/>, using the most efficient method possible to determine that it is a single value.</returns>
		/// <remarks>More efficient than the built in LINQ to SQL "Single" method, because this one takes the minimum number of results necessary to determine if the queryable contains a single value or not.</remarks>
		public static T EnsureSingle<T>(this IQueryable<T> queryable)
		{
			return queryable.Take(2).Single();
		}
	}
}
