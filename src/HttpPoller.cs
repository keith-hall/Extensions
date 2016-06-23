using System;
using System.Net;
using System.Reactive.Linq;

namespace HallLibrary.Extensions
{
	/// <summary>
	/// Contains static methods useful for polling a REST/HTTP server's resources.
	/// </summary>
	public static class HttpPoller
	{
		/// <summary>
		/// Contains the response headers and plain text content from a web request, or details of the Exception that occurred as a result of the request.
		/// </summary>
		public struct ResponseDetails
		{
			public WebHeaderCollection Headers;
			public string Text;
			public WebException Exception;
		}
	
		/// <summary>
		/// Poll a <paramref name="url"/> at the specified <paramref name="frequency"/> and create an observable that will update every time the content recieved changes.
		/// </summary>
		/// <param name="url">The url to poll.</param>
		/// <param name="frequency">The frequency to poll at.</param>
		/// <param name="createWebClient">An optional function to create a WebClient instance, for overriding timeouts, request headers etc.</param>
		/// <returns>An observable containing the response headers and content of the specified url, or details of any exceptions that occurred.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="url" /> or <paramref name="frequency" /> are <c>null</c>.</exception>
		public static IObservable<ResponseDetails> PollURL(string url, TimeSpan frequency, Func<WebClient> createWebClient = null)
		{
			createWebClient = createWebClient ?? (() => new WebClient());
			Func<ResponseDetails> download = () =>
			{
				try
				{
					var wc = createWebClient();
					return new ResponseDetails { Text = wc.DownloadString(url), Headers = wc.ResponseHeaders };
				}
				catch (WebException ex)
				{
					return new ResponseDetails { Exception = ex };
				}
			};
			return Observable.Interval(frequency)
				.Select(l => download())
				.StartWith(download())
				.DistinctUntilChanged(wc => wc.Text).Publish().RefCount();
		}
	}
}
