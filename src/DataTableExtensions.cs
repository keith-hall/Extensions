using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Xml;

namespace HallLibrary.Extensions
{
	/// <summary>
	/// Contains static methods for working with, or converting to/from <see cref="DataTable" />s.
	/// </summary>
	public static class DataTableExtensions
	{
		#region CSV
		/// <summary>
		/// Open a CSV file <paramref name="path"/> and get it's contents respresented in a <see cref="System.Data.DataTable"/>.
		/// </summary>
		/// <param name="path">The path to the CSV file.</param>
		/// <param name="separator">The field separator used in the CSV file.</param>
		/// <returns>A <see cref="System.Data.DataTable"/> representing the contents of the CSV file.</returns>
		public static DataTable CSVToDataTable(string path, string separator)
		{
			var csv = OpenCSV(path, separator).ToList();
			var dt = new DataTable(path);
			foreach (var header in csv.First())
				dt.Columns.Add(header);
			foreach (var values in csv.Skip(1))
				dt.Rows.Add(values);
			return dt;
		}
	
		public static DataTable ToDataTable(this IEnumerable<IDictionary<string, object>> csv, string name = null)
		{
			var result = new DataTable(name);
			if (!csv.Any())
				return result;
	
			var csvList = csv.ToList();
			foreach (var k in (csvList.First() as IDictionary<string, object>).Select(e => e.Key))
				result.Columns.Add(k.ToString());
	
			foreach (var r in csvList)
				result.Rows.Add((r as IDictionary<string, object>).Values.ToArray());
	
			return result;
		}
	
		public static IEnumerable<System.Dynamic.ExpandoObject> FromCSVEnumerable(IEnumerable<IEnumerable<string>> csv)
		{
			using (var iterator = csv.GetEnumerator()) {
				if (! iterator.MoveNext())
					throw new InvalidOperationException(@"No header row in enumerable");
					
				var headers = iterator.Current.ToArray();
				
				while (iterator.MoveNext())
				{
					var row = iterator.Current;
					var obj = new System.Dynamic.ExpandoObject();
					var ret = obj as IDictionary<string, object>;
		
					//x for (int i = 0; i < headers.Length; i++)
					//x 	ret.Add(headers[i], row.ElementAt(i));
					var zipped = headers.Zip(row, (header, value) => new KeyValuePair<string, object>(header, value));
					foreach (var kvp in zipped)
						ret.Add(kvp);
		
					yield return obj;
				}
			}
		}
		
		/// <summary>
		/// Open a CSV file at the specified <paramref name="path"/> using the specified field <paramref name="separator"/>.
		/// </summary>
		/// <param name="path">The path to the CSV file.</param>
		/// <param name="separator">The field separator to use.</param>
		/// <returns>An enumerable containing a <see cref="System.String"/> array all the fields in each row.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="separator"/> is null or empty.</exception>
		public static IEnumerable<string[]> OpenCSV(string path, string separator)
		{ //http://msdn.microsoft.com/en-us/library/microsoft.visualbasic.fileio.textfieldparser.aspx
			using (var tfp = new Microsoft.VisualBasic.FileIO.TextFieldParser(path))
			{
				try
				{
					tfp.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
					tfp.SetDelimiters(separator);
	
					while (!tfp.EndOfData)
						yield return tfp.ReadFields();
				}
				finally
				{
					tfp.Close();
				}
			}
		}
		#endregion
	
		#region SQL
		public static string TableToSQLInsert(this DataTable dt, string tableName, bool createTable)
		{
			var sql = "insert into " + tableName + " (" +
				string.Join(", ", dt.Columns.OfType<DataColumn>().Select(
					col => "[" + col.ColumnName + "]"
				)) + ") values\r\n";
			sql += " (" + string.Join(")\r\n,(", dt.Rows.OfType<DataRow>().Select(
				row => string.Join(", ", dt.Columns.OfType<DataColumn>().Select(
					col => GetValueForSQL(row[col], true)
				)))
			) + ")";
	
			if (createTable)
			{
				// TODO: more efficient way of doing this that doesn't involve calling GetValueForSQL again for each cell, plus to see if the inferred data types for all values in a column are the same, and to use it - i.e. DateTime
				sql = (tableName.StartsWith("@") ? "DECLARE " : "CREATE TABLE ") + tableName + (tableName.StartsWith("@") ? " TABLE " : string.Empty) + " (" + string.Join(
					", ",
					dt.Columns.OfType<DataColumn>().Select(dc => "[" + dc.ColumnName + "] NVARCHAR(" + dt.Rows.OfType<DataRow>().Max(dr => GetValueForSQL(dr[dc], true).Length).ToString() + ")")
					) + ")\r\n" + sql;
			}
	
			return sql;
		}
	
		public static string GetValueForSQL(object value, bool tryConvertFromString)
		{
			if (value == null)
				return @"null";
	
			Func<bool, string> toBoolString = b => b ? @"1" : @"0";
			Func<DateTime, string> toDateString = d => @"'" + d.ToSortableDateTime() + @"'";
	
			if (value is DateTime)
				return toDateString((DateTime)value);
			if (value is bool)
				return toBoolString((bool)value);
			if (value is System.Byte[])
				return @"0x" + (new SoapHexBinary(value as System.Byte[]).ToString());
	
			var val = value.ToString();
			if (value is int || value is float || value is double)
				return val;
	
			if (tryConvertFromString)
			{
				DateTime valAsDate;
				float valAsFloat;
	
				if (DateTime.TryParse(val, out valAsDate))
					return toDateString(valAsDate);
				if (val.Equals(bool.FalseString, StringComparison.InvariantCultureIgnoreCase) || val.Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase))
					return toBoolString(bool.Parse(val));
				if (val.Length < 7 && float.TryParse(val, out valAsFloat)) // check if it is a number stored in a string value
					return val;
			}
			return @"'" + val.Replace(@"'", @"''") + @"'"; // escape the value
		}
		#endregion
	
		#region Data Table Filters
		public static DataTable Filter(this DataTable table, string filter)
		{
			return table.Filter(table.Select(filter));
		}
		
		/// <summary>
		/// Filter the specified <paramref name="rows"/> from the data<paramref name="table"/> into a new <see cref="System.Data.DataTable"/>.
		/// </summary>
		/// <param name="table">The <see cref="System.Data.DataTable"/> to filter.</param>
		/// <param name="rows">The rows to filter.</param>
		/// <returns>A new <see cref="System.Data.DataTable"/> containing the specified <paramref name="rows"/>.</returns>
		public static DataTable Filter(this DataTable table, IEnumerable<DataRow> rows)
		{
			var filtered = table.Clone();
			foreach (var row in rows)
				filtered.Rows.Add(row.ItemArray);
			return filtered;
		}
		#endregion
	
		#region XML
		/// <summary>
		/// Iterate through the specified <see cref="XmlReader" /> and build a <see cref="DataTable"/> from it.
		/// </summary>
		/// <param name="xr">The <see cref="XmlReader" /> to use to build the <see cref="DataTable"/>.</param>
		/// <param name="row">The name of the XML element that represents a row.</param>
		/// <param name="shortNames">Use short column names, as opposed to the path relative to the <paramref name="row"/>.</param>
		/// <param name="includeAttributes">Include attributes in the <see cref="DataTable"/>.</param>
		/// <returns>A <see cref="DataTable"/> representing the XML.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="row"/> is null.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="xr"/> is null.</exception>
		public static DataTable ReadXML(XmlReader xr, string row, bool shortNames, bool includeAttributes)
		{
			if (string.IsNullOrEmpty(row))
				throw new ArgumentNullException("row");
			if (xr == null)
				throw new ArgumentNullException("xr");

			var stack = new Stack<string>();
			var dt = new DataTable();

			Action<string, DataRow> process = (name, relatedRow) =>
			{
				if (xr.HasValue && !string.IsNullOrWhiteSpace(xr.Value))
				{
					var level = stack.TakeWhile(v => !v.Equals(row));
					if (name != null)
						level = Enumerable.Concat(new[] { name }, level);
					var col = string.Join("/", level);
					if (!dt.Columns.Contains(col))
						dt.Columns.Add(col);

					if (relatedRow != null)
						relatedRow.SetField(col, xr.Value);
				}
			};

			DataRow currentRow = null;
			while (xr.Read())
			{
				if (xr.NodeType != XmlNodeType.Comment)
				{
					if (xr.NodeType == XmlNodeType.Element)
					{
						if (String.IsNullOrEmpty(dt.TableName))
							dt.TableName = xr.Name;

						if (xr.Name == row)
						{
							currentRow = dt.NewRow();
							dt.Rows.Add(currentRow);
						}
						if (currentRow != null)
							stack.Push(xr.Name);
					}
					if (currentRow != null && xr.HasAttributes && includeAttributes)
					{
						xr.MoveToFirstAttribute();
						do
						{
							process("@" + xr.Name, currentRow);
						} while (xr.MoveToNextAttribute());
						xr.MoveToElement();
					}
					if (currentRow != null)
					{
						process(null, currentRow);
						if (xr.NodeType == XmlNodeType.EndElement || xr.IsEmptyElement)
						{
							if (xr.Name == row)
								currentRow = null;
							if (stack.Any())
								stack.Pop();
						}
					}
				}
			}
			xr.Close();

			if (shortNames)
			{
				var cols = dt.Columns.OfType<DataColumn>().Select(dc => new { Column = dc, FullName = dc.ColumnName, ShortName = dc.ColumnName.Split('/').First() }).GroupBy(c => c.ShortName).Where(g => g.Count() == 1).Select(g => g.First());
				foreach (var col in cols)
					col.Column.ColumnName = col.ShortName;
			}
			return dt;
		}
		#endregion
	}
}
