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
			Assert.AreEqual(src.CountExceeds(3), true);

			// just doing a Count would give a DivideByZero exception
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
	}
}
