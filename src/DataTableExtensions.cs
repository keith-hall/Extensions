using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Xml;
using System.IO;

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
		/// <param name="containsHeaders">Specifies whether the CSV file contains a header row or not, which will be used to set the DataTable column headings.</param>
		/// <param name="inferTypes">Specifies whether to attempt to determine the type of each column based on it's contents.  Supports dates and numbers.</param>
		/// <returns>A <see cref="System.Data.DataTable"/> representing the contents of the CSV file.</returns>
		public static DataTable CSVToDataTable(string path, string separator = null, bool containsHeaders = true, bool inferTypes = false)
		{
			var csv = OpenCSV(path, separator);
			var dt = new DataTable(path);
			
			var iterator = csv.GetEnumerator();
			if (! iterator.MoveNext())
				return dt;
			
			foreach (var header in iterator.Current)
				dt.Columns.Add(containsHeaders ? header : string.Empty);
			
			if (containsHeaders && !iterator.MoveNext())
				return dt;
			
			dt.BeginLoadData();
			
			do {
				dt.Rows.Add(iterator.Current);
			} while (iterator.MoveNext());
			
			dt.EndLoadData();
			
			if (inferTypes) {
				//x for (var col = 0; col < dt.Columns.Count; col ++)
				// x	dt.Columns[col].ConvertFromString();
				Parallel.ForEach(dt.Columns.OfType<DataColumn>().ToList(), col => dt.Columns[col.ColumnName].ConvertFromString());
			}
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
			result.BeginLoadData();
			foreach (var r in csvList)
				result.Rows.Add((r as IDictionary<string, object>).Values.ToArray());
			result.EndLoadData();
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
		/// <param name="separator">The field separator to use.  If <c>null</c>, it will attempt to determine the field separator automatically.</param>
		/// <returns>An enumerable containing a <see cref="System.String"/> array all the fields in each row.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="path"/> is null or empty.</exception>
		/// <exception cref="InvalidDataException">When <paramref name="separator"/> is <c>null</c> and it is unable to automatically determine the field separator.</exception>
		public static IEnumerable<string[]> OpenCSV(string path, string separator = null)
		{ //http://msdn.microsoft.com/en-us/library/microsoft.visualbasic.fileio.textfieldparser.aspx
			if (string.IsNullOrEmpty(separator))
				separator = DetermineCSVSeparator(path);
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
		
		/// <summary>
		/// Open the specified CSV file and parse a few rows to determine what field separator is used.
		/// </summary>
		/// <param name="path">The path to the CSV file.</param>
		/// <returns>The field separator used in the specified CSV file.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="path"/> is null or empty.</exception>
		/// <exception cref="InvalidDataException">When it is unable to automatically determine the field separator.</exception>
		public static string DetermineCSVSeparator (string path) {
			const int maxLinesToExamine = 3;
			var possibleSeparators = new [] { @",", @";", "\t", @"|" };
			var results = new [] { System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator }.Concat(possibleSeparators).Distinct().Select(s => {
				List<int> c = null;
				try {
					c = OpenCSV(path, s).Take(maxLinesToExamine).Select(r => r.Length).Distinct().ToList();
				} catch (Microsoft.VisualBasic.FileIO.MalformedLineException) {
					
				}
				return new { Separator = s, Contents = c };
			}).Where(r => r.Contents != null);
			var bestMatch = results.SingleOrDefault(r => !r.Contents.CountExceeds(1) && r.Contents.Single() > 1); // if there are multiple distinct values then it can't be the correct field separator, and if it only returns one field then it is also incorrect
			if (bestMatch == null) // if there are 0 or more than 1 result, we cannot be sure what the correct field separator is
				throw new InvalidDataException(@"Unable to determine separator for file: " + path);
			
			return bestMatch.Separator;
		}
		
		public static void ConvertToCSV(this DataTable table, TextWriter writer, string separator = "\t")
		{
			var lines = table.Rows.OfType<DataRow>().Select(row => string.Join(separator,
				table.Columns.OfType<DataColumn>().Select(col =>
					GetValueForCSV(row[col], separator)
				)
			));
			
			writer.WriteLine(string.Join(separator,
				table.Columns.OfType<DataColumn>().Select(c => c.Caption)));
			
			foreach (var line in lines)
				writer.WriteLine(line);
			
			writer.Flush();
		}
		
		public static string GetValueForCSV(object value, string separator)
		{
			const string quote = "\"";
			return GetValueBase(value, string.Empty, string.Empty, s => {
				s = s.Replace(Environment.NewLine, " ").Replace("\r", " ").Replace("\n", " "); // remove line breaks, replace with spaces
				s = s.Replace(quote, @"\" + quote); // replace quotes with a backslash and then a quote
				return s.IndexOf(separator, StringComparison.InvariantCultureIgnoreCase) > -1 ? (quote + s + quote) : s;
			});
		}
		#endregion
		
		internal static object ConvertValueFromString(string value) {
			decimal valAsDecimal;
			if (!(value.StartsWith(@"0") && value.Length > 1) // ignore it if it starts with 0
				&& decimal.TryParse(value, out valAsDecimal)) // check if it is a valid number, use decimal as it keeps the formatting / precision
				return valAsDecimal;
			DateTime valAsDate;
			if (DateTime.TryParse(value, out valAsDate))
				return valAsDate;
			if (value.Equals(bool.FalseString, StringComparison.InvariantCultureIgnoreCase) || value.Equals(bool.TrueString, StringComparison.InvariantCultureIgnoreCase))
				return bool.Parse(value);
			return value;
		}
		
		// infer the data type if same for all values in a column
		public static void ConvertFromString (this DataColumn column) {
			if (! column.DataType.Equals(typeof(string)))
				throw new ArgumentException("Column DataType is not String", "column");
			
			var rows = column.Table.Rows.OfType<DataRow>().Select(row => ConvertValueFromString((string)row[column]));
			var items = new List<object>(column.Table.Rows.Count);
			object firstNonNull = null;
			foreach (var row in rows) {
				if (row != null) {
					firstNonNull = firstNonNull ?? row;
					if (items.Any() && !row.GetType().Equals(firstNonNull.GetType())) { // data type of all items don't match
						items = null;
						break;
					}
				}
				items.Add(row);
			}
			if (items != null && firstNonNull != null) {
				lock (column.Table) {
					var ordinal = column.Ordinal;
					var table = column.Table;
					table.BeginLoadData();
					
					var newType = firstNonNull.GetType();
					if (newType.Equals(typeof(decimal)))
						if (! items.Any(value => value != null && ((decimal)value > int.MaxValue || value.ToString().Contains(@"."))))
							newType = typeof(int);
					
					var newColumn = new DataColumn(column.ColumnName, newType);
					table.Columns.Remove(column);
					
					table.Columns.Add(newColumn);
					newColumn.SetOrdinal(ordinal);
					
					foreach (var row in table.Rows.OfType<DataRow>().Zip(items, (dr, newValue) => Tuple.Create(dr, newValue)))
						row.Item1[newColumn] = row.Item2;
					
					table.EndLoadData();
				}
			}
		}
		
		internal static string GetValueBase(object value, string nullSubstitution, string byteArrayPrefix, Func<string, string> escapeIfNecessary) {
			if (value == null)
				return nullSubstitution;
			
			Func<bool, string> toBoolString = b => escapeIfNecessary(b ? @"1" : @"0");
			Func<DateTime, string> toDateString = d => escapeIfNecessary(d.ToSortableDateTime());
			
			if (value is DateTime)
				return toDateString((DateTime)value);
			if (value is bool)
				return toBoolString((bool)value);
			if (value is Byte[])
				return escapeIfNecessary(byteArrayPrefix ?? string.Empty + (new SoapHexBinary(value as Byte[]).ToString()));
			if (value is string)
				return escapeIfNecessary((string)value);
			
			return escapeIfNecessary(value.ToString());
		}
		
		#region SQL
		public static void TableToSQLInsert(this DataTable dt, string tableName, bool createTable, TextWriter writer)
		{
			if (createTable)
			{
				Func<DataColumn, string> ColumnDataTypeToSQL = col => {
					var underlyingType = Nullable.GetUnderlyingType(col.DataType);
					var rows = col.Table.Rows.OfType<DataRow>();
					var nullable = (underlyingType != null) || col.AllowDBNull || rows.Any(row => row[col] == null);
					
					Func<Func<object, int>, string> getMaxLength = getLength => {
						var max = 1;
						if (rows.Any())
							max = Math.Max(max, rows.Where(dr => dr[col] != null).Max(dr => getLength(dr[col])));
						return @"(" + max.ToString() + @")";
					};
					
					string sql;
					sql = ((underlyingType ?? col.DataType).Name.ToLowerInvariant());
					switch (sql) {
						case "byte[]":
							sql = @"varbinary" + getMaxLength(obj => ((byte[])obj).Length);
							break;
						case "guid":
							sql = @"uniqueidentifier";
							break;
						case "int32":
							sql = @"int";
							break;
						case "int64":
							sql = @"bigint";
							break;
						case "boolean":
							sql = @"bit";
							break;
						case "string":
							sql = @"nvarchar" + getMaxLength(obj => ((string)obj).Length);
							break;
					}
					sql += ((nullable) ? string.Empty : @" not") + @" null";
					return sql;
				};
				const string tableVariableChar = @"@";
				writer.WriteLine((tableName.StartsWith(tableVariableChar) ? @"DECLARE " : @"CREATE TABLE ") + tableName + (tableName.StartsWith(tableVariableChar) ? @" TABLE " : string.Empty) + @" (" +
					string.Join(
						@", ",
						dt.Columns.OfType<DataColumn>().Select(col => @"[" + col.ColumnName + @"] " + ColumnDataTypeToSQL(col))
					) + @")"
				);
			}
			
			writer.WriteLine(@"insert into " + tableName + @" (" +
				string.Join(@", ", dt.Columns.OfType<DataColumn>().Select(
					col => @"[" + col.ColumnName + @"]"
				)) + @") values");
			
			var lines = dt.Rows.OfType<DataRow>().Select(
				row => @"(" + string.Join(@", ", dt.Columns.OfType<DataColumn>().Select(
					col => GetValueForSQL(row[col])
				)) + @")");
			
			bool first = true;
			foreach (var line in lines) {
				if (first) {
					first = false;
					writer.Write(@" ");
				} else
					writer.Write(@",");
				writer.WriteLine(line);
			}
			writer.Flush();
		}
		
		public static string GetValueForSQL(object value)
		{
			return GetValueBase(value, @"null", @"0x", s => {
				if (value is decimal || value is int || value is long || value is float) // if it is a numeric type, no need to escape it
					return s;
				else
					return @"'" + s.Replace(@"'", @"''") + @"'"; // escape the value
			});
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
			filtered.BeginLoadData();
			foreach (var row in rows)
				filtered.Rows.Add(row.ItemArray);
			filtered.EndLoadData();
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
