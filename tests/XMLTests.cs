using System;
using System.Xml.Linq;
using HallLibrary.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Data;
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
			var colNames = dt.Columns.OfType<DataColumn>().Select(c => c.ColumnName);
            Assert.IsTrue(colNames.SequenceEqual(new [] { "#row", "id/@example", "id", "name/last", "name/first" }));
			Assert.IsTrue(dt.Rows.GetValuesInColumn(0).SequenceEqual(new[] { "row", "anotherRow" }));
			Assert.IsTrue(dt.Rows.GetValuesInColumn(1).SequenceEqual(new object[] { "test", DBNull.Value }));
			Assert.IsTrue(dt.Rows.GetValuesInColumn(2).SequenceEqual(new [] { "7", "6" }));
			Assert.IsTrue(dt.Rows.GetValuesInColumn(3).SequenceEqual(new[] { "Bloggs", "Bloggs" }));
			Assert.IsTrue(dt.Rows.GetValuesInColumn(4).SequenceEqual(new [] { "Fred", "Joe" }));
			Assert.AreEqual(dt.TableName, "root");
			
			xe = XElement.Parse(@"<root><row><id example='test'>7</id><name><last>Bloggs</last><first>Fred</first></name></row><row><id>6</id><name><first>Joe</first><last>Bloggs</last></name></row></root>");
			dt = XML.ToDataTable(xe, ".", false, false);
			colNames = dt.Columns.OfType<DataColumn>().Select(c => c.ColumnName);
			Assert.IsTrue(colNames.SequenceEqual(new [] { "id", "last.name", "first.name" }));
			
			Assert.IsTrue(dt.Rows.GetValuesInColumn(0).SequenceEqual(new [] { "7", "6" }));
			Assert.IsTrue(dt.Rows.GetValuesInColumn(1).SequenceEqual(new[] { "Bloggs", "Bloggs" }));
			Assert.IsTrue(dt.Rows.GetValuesInColumn(2).SequenceEqual(new [] { "Fred", "Joe" }));
			Assert.AreEqual(dt.TableName, "row");
			
			dt = XML.ToDataTable(xe, null, false, false);
			Assert.IsTrue(dt.Columns.OfType<DataColumn>().Select(c => c.ColumnName).SequenceEqual(new [] { "id", "last", "first" }));
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
