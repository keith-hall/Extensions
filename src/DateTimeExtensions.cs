public static class DateTimeExtensions {
	public static string ToISO8601String (this DateTime dt) { // ISO-8601 date format
		return dt.ToString(@"yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
	}
}
