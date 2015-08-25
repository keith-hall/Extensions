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
		public void TestDistinct()
		{
			// show that Distinct does not iterate through all elements before returning elements
			Assert.IsFalse(_failAtFifthIteration.Distinct().CountEquals(2));
			Assert.IsTrue(_threeElements.Distinct().CountEquals(3));
		}
	}
}
