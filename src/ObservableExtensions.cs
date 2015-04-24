using System;
using System.Reactive.Linq;

namespace HallLibrary.Extensions
{
	/// <summary>
	/// Contains extension methods for working with observables.
	/// </summary>
	public static class ObservableExtensions
	{
		public static IObservable<TOutput> WithPrevious<TSource, TOutput>(this IObservable<TSource> source, Func<TSource, TSource, TOutput> projection)
		{
			return source.Scan(Tuple.Create(default(TSource), default(TSource)),
				(previous, current) => Tuple.Create(previous.Item2, current))
				.Select(t => projection(t.Item1, t.Item2));
		}
	}
}
