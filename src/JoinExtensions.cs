using System;
using System.Collections.Generic;
using System.Linq;

namespace HallLibrary.Extensions
{
	public static class JoinExtensions
	{
		public static IEnumerable<Tuple<TLeft, TRight>> LeftJoinEach<TLeft, TRight, TJoin>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TJoin> leftJoinOn, Func<TRight, TJoin> rightJoinOn)
		{
			var j = from l  in left
				join r  in right on leftJoinOn(l) equals rightJoinOn(r) into gj
				from jr in gj.DefaultIfEmpty()
				select Tuple.Create(l, jr);
			return j;
		}
	
		public static IEnumerable<Tuple<TLeft, IEnumerable<TRight>>> LeftJoinAll<TLeft, TRight, TJoin>(this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TJoin> leftJoinOn, Func<TRight, TJoin> rightJoinOn)
		{
			var j = from l in left
				join r in right on leftJoinOn(l) equals rightJoinOn(r) into gj
				select Tuple.Create(l, gj.DefaultIfEmpty());
			return j;
		}
		
		// based on http://stackoverflow.com/a/13503860/4473405
		internal static Tuple<HashSet<TKey>, ILookup<TKey, TA>, ILookup<TKey, TB>> FullOuterJoinImpl<TA, TB, TKey>(
			this IEnumerable<TA> a,
			IEnumerable<TB> b,
			Func<TA, TKey> selectKeyA,
			Func<TB, TKey> selectKeyB,
			IEqualityComparer<TKey> cmp = null
		)
		{
			cmp = cmp ?? EqualityComparer<TKey>.Default;
			var alookup = a.ToLookup(selectKeyA, cmp);
			var blookup = b.ToLookup(selectKeyB, cmp);
			
			var keys = new HashSet<TKey>(alookup.Select(p => p.Key), cmp);
			keys.UnionWith(blookup.Select(p => p.Key));
			
			return Tuple.Create(keys, alookup, blookup);
		}
		
		public static IEnumerable<TResult> FullOuterGroupJoin<TA, TB, TKey, TResult>(
			this IEnumerable<TA> a,
			IEnumerable<TB> b,
			Func<TA, TKey> selectKeyA,
			Func<TB, TKey> selectKeyB,
			Func<IEnumerable<TA>, IEnumerable<TB>, TKey, TResult> projection,
			IEqualityComparer<TKey> cmp = null
		)
		{
			var t = FullOuterJoinImpl(a, b, selectKeyA, selectKeyB, cmp);
			var keys = t.Item1;
			var alookup = t.Item2;
			var blookup = t.Item3;
			
			var join = from key in keys
	                   let xa = alookup[key]
	                   let xb = blookup[key]
	                   select projection(xa, xb, key);
			
			return join;
		}
		
		public static IEnumerable<TResult> FullOuterJoin<TA, TB, TKey, TResult>(
			this IEnumerable<TA> a,
			IEnumerable<TB> b,
			Func<TA, TKey> selectKeyA,
			Func<TB, TKey> selectKeyB,
			Func<TA, TB, TKey, TResult> projection,
			TA defaultA = default(TA),
			TB defaultB = default(TB),
			IEqualityComparer<TKey> cmp = null
		)
		{
			var t = FullOuterJoinImpl(a, b, selectKeyA, selectKeyB, cmp);
			var keys = t.Item1;
			var alookup = t.Item2;
			var blookup = t.Item3;
			
			var join = from key in keys
					   from xa in alookup[key].DefaultIfEmpty(defaultA)
					   from xb in blookup[key].DefaultIfEmpty(defaultB)
					   select projection(xa, xb, key);
			
			return join;
		}
	}
}
