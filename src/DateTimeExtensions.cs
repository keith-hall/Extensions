/// <summary>
/// Contains extension methods for working with <see cref="DateTime" />s.
/// </summary>
public static class DateTimeExtensions
{
	/// <summary>
	/// Converts the specified <see cref="DateTime"/> to a <see cref="String"/>, in ISO-8601 format.
	/// </summary>
	/// <param name="dt">The date to convert.</param>
	/// <param name="withT">Whether or not to include the 'T' in the output.  If false, a space will be used instead.</param>
	/// <param name="toUTC">Whether or not to convert the time to UTC or not first.</param>
	/// <returns>An ISO-8601 formatted <see cref="String"/> representation of the specified <see cref="DateTime"/>.</returns>
	public static string ToISO8601String(this DateTime dt, bool withT)
	{
		return dt.ToString(
			Thread.CurrentThread.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern
			.Replace(@"Z'", @".'fff")
			.Replace(@" ", withT ? @"'T'" : @" ")
			+ @"'" + TimeZoneString(dt) + @"'"
		, CultureInfo.InvariantCulture);
	}
	
	public static string TimeZoneString (this DateTime dt) {
		if (dt.Kind == DateTimeKind.Utc)
			return @"Z";
		var diff = System.TimeZoneInfo.Local.BaseUtcOffset;
		var hours = diff.Hours;
		if (System.TimeZoneInfo.Local.IsDaylightSavingTime(dt))
			hours += 1;
		return (hours > 0 ? @"+" : string.Empty) + hours.ToString(@"00")
		       + @":" + diff.Minutes.ToString(@"00");
	}
	
	public static DateTime FromUtc (this DateTime dt) {
		if (dt.Kind == DateTimeKind.Local)
			return dt;
		
		return System.TimeZoneInfo.ConvertTimeFromUtc(dt, System.TimeZoneInfo.Local);
	}
}
