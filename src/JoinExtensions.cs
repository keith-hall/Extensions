public static class JoinExtensions {
	public static IEnumerable<Tuple<TLeft, TRight>> LeftJoinEach<TLeft, TRight, TJoin> (this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TJoin> leftJoinOn, Func<TRight, TJoin> rightJoinOn) {
		var j = from l  in left
                join r  in right on leftJoinOn(l) equals rightJoinOn(r) into gj
                from jr in gj.DefaultIfEmpty()
                select Tuple.Create(l, jr);
		return j;
	}
	
	public static IEnumerable<Tuple<TLeft, IEnumerable<TRight>>> LeftJoinAll<TLeft, TRight, TJoin> (this IEnumerable<TLeft> left, IEnumerable<TRight> right, Func<TLeft, TJoin> leftJoinOn, Func<TRight, TJoin> rightJoinOn) {
		var j = from l  in left
                join r  in right on leftJoinOn(l) equals rightJoinOn(r) into gj
                select Tuple.Create(l, gj.DefaultIfEmpty());
		return j;
	}
}
