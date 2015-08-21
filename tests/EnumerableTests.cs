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
		[TestMethod]
		public void TestCountExceeds()
		{
			var src = Enumerable.Range(1, 4).Concat(new[] { 0 }).Select(s => 1 / s);
			// ReSharper disable once PossibleMultipleEnumeration
			Assert.IsTrue(src.CountExceeds(3));

			// just doing a Count would give a DivideByZero exception - this could also be demonstrated with the [ExpectedException(typeof(DivideByZeroException))] attribute on this method, but we want to test multiple conditions in a single method
			try
			{
				// ReSharper disable once PossibleMultipleEnumeration
				// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
				if (src.Count() > 3)
					Assert.Fail("The above should cause a DivideByZero Exception so this line should never be reached.");
				else
					Assert.Fail("Count is greater than 3 so this line should never be reached.");
			}
			catch (DivideByZeroException)
			{

			}
			catch (Exception)
			{
				Assert.Fail("The above should cause a DivideByZero Exception so this line should never be reached.");
			}
			
			src = Enumerable.Range(1, 2);
			Assert.IsFalse(src.CountExceeds(2));
		}
		
		[TestMethod]
		public void TestCountEquals()
		{
			var src = Enumerable.Range(1, 3).Concat(Enumerable.Range(0, 1).Select(s => 1 / s));
			Assert.IsFalse(src.Distinct().CountEquals(2));
			
			src = Enumerable.Range(1, 3);
			Assert.IsTrue(src.CountEquals(3));
		}
	}
}
