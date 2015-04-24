using System;
using System.Reactive.Linq;

namespace HallLibrary.Extensions
{
	/// <summary>
	/// Contains extension methods for working with observables.
	/// </summary>
	public static class ObservableExtensions
	{
		/// <summary>
		/// Returns the current value of an observable with the previous value.
		/// </summary>
		/// <typeparam name="TSource">The source type.</typeparam>
		/// <typeparam name="TOutput">The output type after the <paramref name="projection" /> has been applied.</typeparam>
		/// <param name="source">The observable.</param>
		/// <param name="projection">The projection to apply.</param>
		/// <returns>The current observable value with the previous value.</returns>
		public static IObservable<TOutput> WithPrevious<TSource, TOutput>(this IObservable<TSource> source, Func<TSource, TSource, TOutput> projection)
		{
			return source.Scan(Tuple.Create(default(TSource), default(TSource)),
				(previous, current) => Tuple.Create(previous.Item2, current))
				.Select(t => projection(t.Item1, t.Item2));
		}
	}
}
