using System;
using System.Globalization;
using System.Threading;

namespace HallLibrary.Extensions
{
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
		/// <returns>An ISO-8601 formatted <see cref="String"/> representation of the specified <see cref="DateTime"/>.</returns>
		public static string ToISO8601String(this DateTime dt, bool withT)
		{
			return dt.ToString(
				dt.ToSortableDateTime()
				.Replace(@" ", withT ? @"'T'" : @" ")
				+ @"'" + TimeZoneString(dt) + @"'"
			, CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Converts the specified <see cref="DateTime"/> to a <see cref="String"/>, in a universal sortable format.
		/// </summary>
		/// <param name="dt">The date to convert.</param>
		/// <returns>A sortable formatted <see cref="String"/> representation of the specified <see cref="DateTime"/>.</returns>
		public static string ToSortableDateTime (this DateTime dt) {
			return dt.ToString(
				Thread.CurrentThread.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern
			        .Replace(@"Z'", @".'fff")
			       );
		}
		
		/// <summary>
		/// Gets the local time zone offset <see cref="String"/> from UTC for the specified <see cref="DateTime"/>.
		/// </summary>
		/// <param name="dt">The date to get the local timezone offset <see cref="String"/> for.</param>
		/// <returns>A <see cref="String"/> representation of the local timezone offset for the specified <see cref="DateTime"/>.</returns>
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
		
		/// <summary>
		/// Convert the specified <see cref="DateTime"/> from UTC to the Local timezone.
		/// </summary>
		/// <param name="dt">The date to convert to local time.</param>
		/// <returns>The specified UTC <see cref="DateTime"/> converted to the Local timezone.</returns>
		public static DateTime FromUniversalTime (this DateTime dt) {
			if (dt.Kind == DateTimeKind.Local)
				return dt;
			
			return System.TimeZoneInfo.ConvertTimeFromUtc(dt, System.TimeZoneInfo.Local);
		}
	}
}
