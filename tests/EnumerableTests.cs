using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HallLibrary.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
	[TestClass]
	public class EnumerableTests
	{
		private IEnumerable<int> _failAtFifthIteration = Enumerable.Range(1, 4).Concat(new[] { 0 }.Select(s => 1 / s));
		private IEnumerable<int> _threeElements = Enumerable.Range(1, 3);
		
		[TestMethod]
		public void TestCountExceeds()
		{
			Assert.IsTrue(_failAtFifthIteration.CountExceeds(3));
			
			Assert.IsTrue(_threeElements.CountExceeds(2));
			Assert.IsFalse(_threeElements.CountExceeds(3));
			Assert.IsFalse(_threeElements.CountExceeds(4));
		}
		
		[TestMethod]
		[ExpectedException(typeof(DivideByZeroException))]
		public void TestCountException()
		{
			// show that Count iterates through all elements, which is why my extension methods are better
			// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
			if (_failAtFifthIteration.Count() > 3)
				Assert.Fail("The above should cause a DivideByZero Exception so this line should never be reached.");
			else
				Assert.Fail("Count is greater than 3 so this line should never be reached.");
		}
		
		[TestMethod]
		public void TestCountEquals()
		{
			Assert.IsFalse(_failAtFifthIteration.CountEquals(2));
			Assert.IsTrue(_threeElements.CountEquals(3));
			Assert.IsFalse(_threeElements.CountEquals(2));
		}
		
		[TestMethod]
		public void TestCountIsLessThan()
		{
			Assert.IsFalse(_failAtFifthIteration.CountIsLessThan(3));
			Assert.IsTrue(_threeElements.CountIsLessThan(4));
			Assert.IsFalse(_threeElements.CountIsLessThan(3));
		}
		
		[TestMethod]
		public void TestCountEqualsWithDistinct()
		{
			// show that Distinct does not iterate through all elements before returning elements
			Assert.IsFalse(_failAtFifthIteration.Distinct().CountEquals(2));
			Assert.IsTrue(_threeElements.Distinct().CountEquals(3));
		}
		
		[TestMethod]
		public void TestGroupRank()
		{
			var scores = new object[] { "Jane", 18, "Joe", 12, "Fred", 18, "Jill", 21, "Jim", 15 };
			var zipped = scores.OfType<string>().Zip(scores.OfType<int>(), Tuple.Create);
			
			var grouped = zipped.GroupBy(t => t.Item2).OrderByDescending(g => g.Key);
			var ranked = grouped.Rank(false, Tuple.Create);
			Assert.IsTrue(ranked.Select(t => t.Item3).SequenceEqual(new[] { 1, 2, 2, 4, 5 }));
			//Assert.IsTrue(ranked.Select(t => t.Item2.Item1).SequenceEqual(new[] { "Jill", "Jane", "Fred", "Jim", "Joe" }));
			
			ranked = grouped.Rank(true, Tuple.Create);
			Assert.IsTrue(ranked.Select(t => t.Item3).SequenceEqual(new[] { 1, 2, 2, 3, 4 }));
			//Assert.IsTrue(ranked.Select(t => t.Item2.Item1).SequenceEqual(new[] { "Jill", "Jane", "Fred", "Jim", "Joe" }));
		}
	}
}
