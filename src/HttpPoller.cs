public static class HttpPoller {
	public struct ResponseDetails {
		public WebHeaderCollection Headers;
		public string Text;
	}
	
	public static IObservable<ResponseDetails> PollURL (string url, TimeSpan frequency, Func<WebClient> createWebClient = null) {
		createWebClient = createWebClient ?? (() => new WebClient());
		Func<ResponseDetails> download = () => {
			var wc = createWebClient();
			return new ResponseDetails { Text = wc.DownloadString(url), Headers = wc.ResponseHeaders };
		};
		return Observable.Interval(frequency)
			.Select(l => download())
			.StartWith(download())
			.DistinctUntilChanged(wc => wc.Text).Publish().RefCount();
	}
}
