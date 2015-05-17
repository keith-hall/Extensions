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
		{ // http://www.zerobugbuild.com/?p=213
			return source.Scan(Tuple.Create(default(TSource), default(TSource)),
				(previous, current) => Tuple.Create(previous.Item2, current))
				.Select(t => projection(t.Item1, t.Item2));
		}
		
		/// <summary>
		/// Ensure that the subscription to the <paramref name="observable" /> is re-subscribed to on error.
		/// </summary>
		/// <typeparam name="T">The type of data the observable contains.</typeparam>
		/// <param name="observable">The observable to re-subscribe to on error.</param>
		/// <param name="onNext">Action to invoke for each element in the <paramref name="observable" /> sequence.</param>
		/// <param name="onError">Action to invoke for each error that occurs in the <paramref name="observable" /> sequence.</param>
		public static void ReSubscribeOnError<T> (this IObservable<T> observable, Action<T> onNext, Action<Exception> onError = null)
		{
			Action sub = null;
			sub = () => observable.Subscribe(onNext, ex => { if (onError != null) onError(ex); sub(); });
			sub();
		}
	}
}
