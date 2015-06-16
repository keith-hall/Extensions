using System;
using System.Collections.Generic;
using System.Linq;

namespace HallLibrary.Extensions
{
	public static class RelativeTime
	{
		public static string GetRelativeTime (DateTime baseDate, bool includeWeeks = true) {
			var relative = GetRelativeTimeComponents(baseDate, includeWeeks).ToList();
			var future = relative.Any(v => v.Value < 0);
			//if (relative.TakeWhile(v => v.Key != IntervalType.Day).All(v => v.Value == 0) && relative.Any(v => v.Key == IntervalType.Day && Math.Abs(v.Value) == 1)) // exactly one day (no weeks/months/years), time is allowed // TODO: check is the next/previous day and not taken over to the one after/before by the time component...
			//	return string.Format(future ? "Tomorrow at {0}" : "Yesterday at {0}", baseDate.ToShortTimeString());
			return (future ? "in " : string.Empty) + string.Join(" and ", relative.SkipWhile(r => r.Value == 0).Take(2).Where(r => r.Value != 0).Select(r => string.Format("{0} {1}{2}", Math.Abs(r.Value), r.Key.ToString().ToLower(), Math.Abs(r.Value) == 1 ? string.Empty : "s"))) + (future ? string.Empty : " ago");
		}
		
		public static IEnumerable<KeyValuePair<IntervalType, long>> GetRelativeTimeComponents (DateTime baseDate, bool includeWeeks) {
			DateTime relativeToDate = DateTime.Now;
			bool future;
			if (future = baseDate > relativeToDate) {
				var tmp = relativeToDate;
				relativeToDate = baseDate;
				baseDate = tmp;
			}
			return GetRelativeTimeComponents(baseDate, relativeToDate, includeWeeks).Select (v => future ? TupleKVP(v.Key, -v.Value) : v);
		}
		
		public enum IntervalType {
			Year,
			Month,
			Week,
			Day,
			Hour,
			Minute,
			Second
		}
		
		// as constructors don't allow type inference, we use a static method for creating a KeyValuePair
		public static KeyValuePair<T1, T2> TupleKVP<T1, T2> (T1 key, T2 value) { // TODO: move to another class
			return new KeyValuePair<T1, T2>(key, value);
		}
		
		private static long CalculateDifference (DateTime date1, DateTime date2, Func<DateTime, int> getMajorUnit, Func<DateTime, long> getMinorUnit, long max = 0) {
			var value = max + getMajorUnit(date1) - getMajorUnit(date2) - ((getMinorUnit(date1) >= getMinorUnit(date2)) ? 0 : 1);
			if (max > 0)
				value %= max;
			return value;
		}
		
		public static IEnumerable<KeyValuePair<IntervalType, long>> GetRelativeTimeComponents (DateTime baseDate, DateTime relativeToDate, bool includeWeeks) {
			if (baseDate > relativeToDate)
				throw new ArgumentOutOfRangeException();
			
			yield return TupleKVP(IntervalType.Year , CalculateDifference(relativeToDate, baseDate, d => d.Year , d => d.Month));
			yield return TupleKVP(IntervalType.Month, CalculateDifference(relativeToDate, baseDate, d => d.Month, d => d.Day, 12));
			
			var days = CalculateDifference(relativeToDate, baseDate, d => d.Day, d => d.TimeOfDay.Ticks, DateTime.DaysInMonth(baseDate.Year, baseDate.Month));
			var weeks = includeWeeks ? days / 7 : 0;
			days -= weeks * 7;
			
			if (weeks > 0)
				yield return TupleKVP(IntervalType.Week, weeks);
			
			yield return TupleKVP(IntervalType.Day,    days);
			yield return TupleKVP(IntervalType.Hour,   CalculateDifference(relativeToDate, baseDate, d => d.Hour  , d => d.Minute     , 24));
			yield return TupleKVP(IntervalType.Minute, CalculateDifference(relativeToDate, baseDate, d => d.Minute, d => d.Second     , 60));
			yield return TupleKVP(IntervalType.Second, CalculateDifference(relativeToDate, baseDate, d => d.Second, d => d.Millisecond, 60));
		}
		
		
		// TODO: return an observable, so that it remains correct. it should update at smallest time unit i.e. seconds, then minutes etc... and eventually months and years
	}
}
