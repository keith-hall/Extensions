using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HallLibrary.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
	[TestClass]
	public class StringTests
	{
		[TestMethod]
		public void TestIndexOfEnd()
		{
			Assert.AreEqual(@"hello world".IndexOfEnd(@"hello"), 5);
			Assert.AreEqual(@"hello world".IndexOfEnd(@"o"), 5);
			Assert.AreEqual(@"hello world".IndexOfEnd(@"w"), 7);
		}

		[TestMethod]
		public void TestAllIndexesOf()
		{
			var all = @"hello world".AllSortedIndexesOf(new[]
													   {
														   @"o",
														   @"e",
														   @"l"
													   });
			Assert.IsTrue(all.SequenceEqual(new[]
											{
												new KeyValuePair<string, int>(@"e", 1),
												new KeyValuePair<string, int>(@"l", 2),
												new KeyValuePair<string, int>(@"l", 3),
												new KeyValuePair<string, int>(@"o", 4),
												new KeyValuePair<string, int>(@"o", 7),
												new KeyValuePair<string, int>(@"l", 9)
											}));
		}

		[TestMethod]
		public void TestCountOccurences()
		{
			Assert.AreEqual(@"hello world".CountOccurrences(Enumerable.Repeat(@"l", 1)), 3);
		}

		[TestMethod]
		public void TestTextBefore()
		{
			Assert.AreEqual(@"hello world".TextBefore(@" "), @"hello");
			Assert.AreEqual(@"hello world".TextBefore(@"this"), string.Empty);
		}

		[TestMethod]
		public void TestTextAfter()
		{
			Assert.AreEqual(@"hello world".TextAfter(@" "), @"world");
			Assert.AreEqual(@"hello world".TextAfter(@"this"), string.Empty);
		}

		[TestMethod]
		public void TestTextBetween()
		{
			Assert.IsTrue(@"i.e. <ul><li>test 1</li><!-- comment --><li>text 2</li><li>testing</li></ul> - get li texts".AllTextBetween("<li>", "</li>").SequenceEqual(new[]
																																									   {
																																										   @"test 1",
																																										   @"text 2",
																																										   @"testing"
																																									   }));

			// ignores start match that has no corresponding end match
			Assert.IsTrue(@"i.e. <ul><li>test 1</li><!-- comment --><li>text 2</li><li>testing</li></ul> - get <li> texts".AllTextBetween("<li>", "</li>").SequenceEqual(new[]
																																										 {
																																											 @"test 1",
																																											 @"text 2",
																																											 @"testing"
																																										 }));

			// some matches contain the start string
			Assert.IsTrue(@"<li>test<li>blah</li>text</li><li>2</li>".AllTextBetween("<li>", "</li>").SequenceEqual(new[]
																													{
																														@"test<li>blah",
																														@"2"
																													}));

			// sometimes the start and end match strings are identical
			Assert.IsTrue(@"hello;world;testing;text;test".AllTextBetween(";", ";").SequenceEqual(new[]
																								  {
																									  @"world",
																									  @"text"
																								  }));
		}
	}
}
