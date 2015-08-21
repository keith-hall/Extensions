using System;
using System.Xml.Linq;
using HallLibrary.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
	[TestClass]
	public class XMLTests
	{
		[TestMethod]
		public void TestToDataTable()
		{
			var xe = XElement.Parse(@"<root><row><id example='test'>7</id><name><last>Bloggs</last><first>Fred</first></name></row><anotherRow><id>6</id><name><first>Joe</first><last>Bloggs</last></name></anotherRow></root>");
			var dt = XML.ToDataTable(xe, "/", true, true);
			Assert.IsTrue(dt.Columns.OfType<DataColumn>().Select(c => c.ColumnName).SequenceEqual(new [] { "id/@example", "id", "name/first", "name/last" }));
			
			Assert.IsTrue(dt.Rows.GetValuesInColumn(0).SequenceEqual(new object[] { "test", DBNull.Value }));
			Assert.IsTrue(dt.Rows.GetValuesInColumn(1).SequenceEqual(new [] { "7", "6" }));
			Assert.IsTrue(dt.Rows.GetValuesInColumn(2).SequenceEqual(new [] { "Fred", "Joe" }));
			Assert.IsTrue(dt.Rows.GetValuesInColumn(3).SequenceEqual(new [] { "Bloggs", "Bloggs" }));
			Assert.AreEqual(dt.TableName, "root");
			
			xe = XElement.Parse(@"<root><row><id example='test'>7</id><name><last>Bloggs</last><first>Fred</first></name></row><row><id>6</id><name><first>Joe</first><last>Bloggs</last></name></row></root>");
			dt = XML.ToDataTable(xe, ".", false, false);
			Assert.IsTrue(dt.Columns.OfType<DataColumn>().Select(c => c.ColumnName).SequenceEqual(new [] { "id", "first.name", "last.name" }));
			
			Assert.IsTrue(dt.Rows.GetValuesInColumn(0).SequenceEqual(new [] { "7", "6" }));
			Assert.IsTrue(dt.Rows.GetValuesInColumn(1).SequenceEqual(new [] { "Fred", "Joe" }));
			Assert.IsTrue(dt.Rows.GetValuesInColumn(2).SequenceEqual(new [] { "Bloggs", "Bloggs" }));
			Assert.AreEqual(dt.TableName, "row");
			
			dt = XML.ToDataTable(xe, null, false, false);
			Assert.IsTrue(dt.Columns.OfType<DataColumn>().Select(c => c.ColumnName).SequenceEqual(new [] { "id", "first", "last" }));
		}
		
		[TestMethod]
		public void TestToDataTableDuplicateColumnNames()
		{
			var xe = XElement.Parse(@"<root><row><client><id>1</id><name>Joe Bloggs</name></client><address><id>459</id><country>Ireland</country></address></row><row><client><id>2</id><name>Fred Bloggs</name></client><address><id>214</id><country>Wales</country></address></row></root>");
			var dt = XML.ToDataTable(xe, null, false, false);
			Assert.IsTrue(dt.Columns.OfType<DataColumn>().Select(c => c.ColumnName).SequenceEqual(new [] { "id/client", "name", "id/address", "country" }));
		}
	}
}
