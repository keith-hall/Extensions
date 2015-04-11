public static class StringExtensions {
	public static int IndexOfEnd (this string value, string find, int? start = null) {
		var pos = value.IndexOf(find, start.HasValue ? start.Value : 0);
		if (pos > -1)
			pos += find.Length;
		return pos;
	}
	
	public static IEnumerable<KeyValuePair<string, int>> AllIndexesOf (this string value, IEnumerable<string> find) {
		foreach (var search in find) {
			var pos = 0;
			while ((pos = value.IndexOf(search, pos)) > -1) {
				yield return new KeyValuePair<string, int>(search, pos);
				pos += search.Length;
			}
		}
	}
	
	public static IEnumerable<KeyValuePair<string, int>> AllSortedIndexesOf (this string value, IEnumerable<string> find) {
		var indexes = AllIndexesOf(value, find);
		return indexes.OrderBy(i => i.Value);
	}
	
	public static int CountOccurrences (this string value, IEnumerable<string> find) {
		return value.AllIndexesOf(find).Count();
	}
	
	public static string TextBefore (this string value, string find) {
		var pos = value.IndexOf(find);
		if (pos == -1)
			pos = 0;
		return value.Substring(0, pos);
	}
	
	public static string TextAfter (this string value, string find) {
		var pos = value.IndexOfEnd(find);
		if (pos == -1)
			pos = value.Length;
		return value.Substring(pos);
	}
	
	public static string TextBetween (this string value, string start, string end) {
		return value.TextAfter(start).TextBefore(end);
	}
	
	public static IEnumerable<string> AllTextBetween (this string value, string start, string end) {
		// get all indexes of the start and end tokens, sorted in order
		var results = value.AllSortedIndexesOf(new [] { start, end });
		var nextIsStart = true; // first we want a start token
		var startpos = -1;
		// for each occurrence
		foreach (var v in results) {
			// ignore the result if it isn't what we need next, or if it occurs before the previous token ends
			if (v.Key == (nextIsStart ? start : end) && v.Value >= startpos) {
				if (nextIsStart) {
					startpos = v.Value + start.Length;
					nextIsStart = false;
				} else {
					yield return value.Substring(startpos, v.Value - startpos);
					nextIsStart = true;
					startpos = v.Value + end.Length;
				}
			}
		}
	}
}
