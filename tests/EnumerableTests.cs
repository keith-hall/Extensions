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
			var src = Enumerable.Range(1, 4).Concat(new [] { 0 });
			Assert.AreEqual(src.Select(s => 1 / s).CountExceeds(3), true);
		}
	}
}
