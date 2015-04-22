using System;
using System.Net;
using System.Reactive.Linq;

namespace HallLibrary.Extensions
{
	public static class HttpPoller
	{
		public struct ResponseDetails
		{
			public WebHeaderCollection Headers;
			public string Text;
		}
		
		/// <summary>
		/// Poll a <paramref name="url"/> at the specified <paramref name="frequency"/> and create an observable that will update every time the content recieved changes.
		/// </summary>
		/// <param name="url">The url to poll.</param>
		/// <param name="frequency">The frequency to poll at.</param>
		/// <param name="createWebClient">An optional function to create a WebClient instance, for overriding timeouts, request headers etc.</param>
		/// <returns>An observable containing the response headers and content of the specified url.</returns>
		public static IObservable<ResponseDetails> PollURL(string url, TimeSpan frequency, Func<WebClient> createWebClient = null)
		{
			createWebClient = createWebClient ?? (() => new WebClient());
			Func<ResponseDetails> download = () =>
			{
				var wc = createWebClient();
				return new ResponseDetails { Text = wc.DownloadString(url), Headers = wc.ResponseHeaders };
			};
			return Observable.Interval(frequency)
				.Select(l => download())
				.StartWith(download())
				.DistinctUntilChanged(wc => wc.Text).Publish().RefCount();
		}
	}
}
