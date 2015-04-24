using System;
using System.Globalization;

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
		/// <param name="toUTC">Whether or not to convert the time to UTC or not first.</param>
		/// <returns>An ISO-8601 formatted <see cref="String"/> representation of the specified <see cref="DateTime"/>.</returns>
		public static string ToISO8601String(this DateTime dt, bool withT, bool toUTC)
		{ // ISO-8601 date format
			var utc = dt.ToUniversalTime();
			double difference;
			string offset = string.Empty;
			if (!toUTC && (difference = dt.Subtract(utc).TotalMinutes) != 0) {
				var diff = new TimeSpan(0, (int)difference, 0);
				offset = (diff.Hours > 0 ? @"+" : string.Empty) + diff.Hours.ToString(@"00");
				offset += @":" + diff.Minutes.ToString(@"00");
			} else {
				dt = utc;
				offset = @"Z";
			}
			var formatted = dt.ToString(@"yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
			if (!withT)
				formatted = formatted.Replace('T', ' ');
			return formatted + offset;
		}
	}
}
