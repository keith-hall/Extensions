using System;
using HallLibrary.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
	[TestClass]
	public class NumberTests
	{
		[TestMethod]
		public void TestNumberFormatting()
		{
			var numbers = new [] {
				"1234", "1,234",
				"123", "123",
				"1234.56", "1,234.56",
				"12345678.90", "12,345,678.90",
				"12345678.9012345", "12,345,678.9012345",
				"-1234.56", "-1,234.56"
			};
			for (var i = 0; i < numbers.Length; i += 2) {
				Assert.AreEqual(NumberFormatting.AddThousandsSeparators(numbers[i]), numbers[i + 1]);
			}
		}
	}
}
