using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Xml;
using System.IO;
using System.Threading.Tasks;

namespace HallLibrary.Extensions
{
	/// <summary>
	/// Contains static methods for working with, or converting to/from <see cref="DataTable" />s.
	/// </summary>
	public static class DataTableExtensions
	{
		#region CSV
		/// <summary>
		/// Write the contents of the <see cref="DataTable" /> <paramref name="table" /> to <paramref name="writer" />.
		/// </summary>
		/// <param name="table">The <see cref="DataTable" /> whose contents to write.</param>
		/// <param name="writer">The <see cref="TextWriter" /> to output the contents to.</param>
		/// <param name="separator">The field separator to use in the CSV output.</param>
		/// <param name="includeColumnNamesAsHeader">Whether to include a header row with the column names from the <paramref name="table" /> in the CSV.</param>
		public static void WriteToCSV(this DataTable table, TextWriter writer, string separator = "\t", bool includeColumnNamesAsHeader = true)
		{
			CSV.Write(
				includeColumnNamesAsHeader ? table.Columns.OfType<DataColumn>().Select(c => c.Caption) : null,
				table.Rows.OfType<DataRow>().Select(row => row.ItemArray),
				writer,
				separator
			);
		}
		#endregion
		
		#region DataRowCollection Extensions
		public static IEnumerable<object> GetValuesInColumn (this DataRowCollection rows, DataColumn col)
		{
			return rows.OfType<DataRow>().Select(dr => dr[col]);
		}
		
		public static IEnumerable<object> GetValuesInColumn (this DataRowCollection rows, string columnName)
		{
			return rows.OfType<DataRow>().Select(dr => dr[columnName]);
		}
		
		// could return data from an unexpected column if the data table's columns are modified...
		public static IEnumerable<object> GetValuesInColumn (this DataRowCollection rows, int columnOrdinal)
		{
			return rows.OfType<DataRow>().Select(dr => dr[columnOrdinal]);
		}
		#endregion
		
		public static DataTable PivotData(DataTable source, IEnumerable<string> columnsToPivot, IEnumerable<string> columnsToAggregate, Func<string, object, object, object> aggregate)
		{
			columnsToPivot = columnsToPivot.ToArray();
			columnsToAggregate = columnsToAggregate.ToArray();
			var allColsToRemove = columnsToPivot.Concat(columnsToAggregate).ToArray();
			var copy = source.DefaultView.ToTable(true, source.Columns.OfType<DataColumn>().Select(c => c.ColumnName).Where(c => !allColsToRemove.Contains(c)).ToArray());
			copy.PrimaryKey = copy.Columns.OfType<DataColumn>().ToArray();

			var unique = columnsToPivot.Select(c => source.Rows.OfType<DataRow>().Select(dr => c + ": " + dr[c].ToString()).Distinct().OrderByNatural(s => s));
			var newCols = unique.Select(s => s.Select(v => columnsToAggregate.Select(c => new DataColumn(v + " - " + c, source.Columns[c].DataType)))).SelectMany(cs => cs.SelectMany(c => c)).ToArray();
			copy.Columns.AddRange(newCols);

			foreach (var row in source.Rows.OfType<DataRow>())
			{
				var existingRow = copy.Rows.Find(copy.PrimaryKey.Select(c => row[c.ColumnName]).ToArray());
				var pivotValues = columnsToPivot.Select(c => Tuple.Create(c, row[c])).ToArray();
				foreach (var col in columnsToAggregate)
				{
					foreach (var value in pivotValues)
					{
						var pivotedColName = value.Item1 + ": " + value.Item2.ToString() + " - " + col;
						existingRow.SetField(pivotedColName, aggregate(col, existingRow[pivotedColName], row[col]));
					}
				}
			}

			return copy;
		}
		
		#region Conversion
		public static void ConvertColumnsFromString (this DataTable dt, bool toMixed)
		{
			//Parallel.ForEach(dt.Columns.OfType<DataColumn>().Where(col => col.DataType == typeof(string)).ToList(), col => {
			foreach (var col in dt.Columns.OfType<DataColumn>().Where(col => col.DataType == typeof(string)).ToList()) {
				try {
					dt.Columns[col.ColumnName].ConvertFromString(toMixed);
				} catch (InvalidOperationException) {
				}
			}
			//});
		}
		
		/// <summary>
		/// Apply the given <paramref name="projection" /> to all values in a <paramref name="column" /> and replace it with a new column with the relevant datatype.
		/// </summary>
		/// <param name="column">The column to apply the <paramref name="projection" /> on.</param>
		/// <param name="projection">The projection to apply to each value in the column.</param>
		/// <param name="toMixed">If false, each value must be of the same type after projection.  If true, the column DataType becomes object.</param>
		/// <exception cref="ArgumentNullException">The specified <paramref name="column" /> is <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException">The <paramref name="projection" /> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException"><paramref name="toMixed" /> is false and the specified <paramref name="column" /> contains multiple types after <paramref name="projection" />.</exception>
		public static void Convert (this DataColumn column, Func<object, object> projection, bool toMixed)
		{
			var cells = column.Table.Rows.GetValuesInColumn(column).Select(projection);
			var items = new List<object>(column.Table.Rows.Count);
			Type newType = null;
			foreach (var cell in cells) {
				if (cell != null && !(cell is DBNull) && newType != typeof(object)) {
					newType = newType ?? cell.GetType();
					if (!cell.GetType().Equals(newType)) { // data type of all items don't match
						if (toMixed)
							newType = typeof(object);
						else
							throw new InvalidOperationException("Not all items are of the same type after projection");
					}
				}
				items.Add(cell);
			}
			if (newType != null) {
				lock (column.Table) {
					var ordinal = column.Ordinal;
					var table = column.Table;
					table.BeginLoadData();
					
					if (newType.Equals(typeof(decimal)))
						if (! items.Any(value => value != null && value != DBNull.Value && ((decimal)value > int.MaxValue || value.ToString().Contains(@"."))))
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
		
		/// <summary>
		/// Infer the data type for each value in the specified <paramref name="column" /> containing <see cref="String" />s.
		/// </summary>
		/// <param name="column">The column containing <see cref="String" />s for which to infer the datatype.</param>
		/// <param name="toMixed">If false, each value converted from string must be of the same type.  If true, the column DataType becomes object.</param>
		/// <exception cref="ArgumentException">The DataType of the specified <paramref name="column" /> is not <see cref="String" />.</exception>
		/// <exception cref="ArgumentNullException">The specified <paramref name="column" /> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException"><paramref name="toMixed" /> is false and the specified <paramref name="column" /> contains multiple types when converting from <see cref="String" />.</exception>
		public static void ConvertFromString (this DataColumn column, bool toMixed)
		{
			if (! column.DataType.Equals(typeof(string)))
				throw new ArgumentException("Column DataType is not String", nameof(column));
			
			Convert(column, value => value is DBNull ? DBNull.Value : ToOrFromString.ConvertValueFromString((string)value), toMixed);
		}
		#endregion
		
		#region SQL
		public static void TableToSQLInsert(this DataTable dt, string tableName, bool createTable, TextWriter writer)
		{
			Func<string, string> escapeIfNecessary = name => {
				// TODO: split by dot and then check each substring for square brackets, to allow for cross-database create table
				if (!name.StartsWith(@"[")) // TODO: could also check if contains invalid characters, rather than always escaping...
					return @"[" + name + @"]";
				else
					return name;
			};
			
			const string tableVariableChar = @"@";
			if (createTable)
			{
				Func<DataColumn, string> ColumnDataTypeToSQL = col => {
					var underlyingType = Nullable.GetUnderlyingType(col.DataType);
					var rows = col.Table.Rows.OfType<DataRow>();
					var nullable = (underlyingType != null) || col.AllowDBNull || rows.Any(row => row[col] == null);
					
					Func<Func<object, int>, string> getMaxLength = getLength => {
						var max = 1;
						if (rows.Any())
							max = Math.Max(max, rows.Where(dr => dr[col] != null && !(dr[col] is DBNull)).Max(dr => getLength(dr[col])));
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
				writer.WriteLine((tableName.StartsWith(tableVariableChar) ? @"DECLARE " + tableName : @"CREATE TABLE " + escapeIfNecessary(tableName)) + (tableName.StartsWith(tableVariableChar) ? @" TABLE " : string.Empty) + @" (" +
					string.Join(
						@", ",
						dt.Columns.OfType<DataColumn>().Select(col => escapeIfNecessary(col.ColumnName) + @" " + ColumnDataTypeToSQL(col))
					) + @")"
				);
			}
			
			writer.WriteLine(@"insert into " + (tableName.StartsWith(tableVariableChar) ? tableName : escapeIfNecessary(tableName)) + @" (" +
				string.Join(@", ", dt.Columns.OfType<DataColumn>().Select(
					col => escapeIfNecessary(col.ColumnName)
				)) + @") values");
			
			var lines = dt.Rows.OfType<DataRow>().Select(
				row => @"(" + string.Join(@", ", dt.Columns.OfType<DataColumn>().Select(
					col => ToOrFromString.GetValueForSQL(row[col])
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
		#endregion
	
		#region Data Table Filters
		public static DataTable Filter(this DataTable table, string filter)
		{
			return table.Filter(table.Select(filter));
		}
		
		/// <summary>
		/// Filter the specified <paramref name="rows" /> from the <see cref="DataTable" /> <paramref name="table" /> into a new <see cref="DataTable" />.
		/// </summary>
		/// <param name="table">The <see cref="DataTable" /> to filter.</param>
		/// <param name="rows">The rows to filter.</param>
		/// <returns>A new <see cref="DataTable" /> containing the specified <paramref name="rows" />.</returns>
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
	}


	public static class CSV
	{
		/// <summary>
		/// Open a CSV file at the specified <paramref name="path" /> using the specified field <paramref name="separator" />.
		/// </summary>
		/// <param name="path">The path to the CSV file.</param>
		/// <param name="separator">The field separator to use.  If <c>null</c>, it will attempt to determine the field separator automatically.</param>
		/// <returns>An enumerable containing a <see cref="String" /> array all the fields in each row.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="path" /> is null or empty.</exception>
		/// <exception cref="InvalidDataException">When <paramref name="separator" /> is <c>null</c> and it is unable to automatically determine the field separator.</exception>
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
		/// <exception cref="ArgumentNullException"><paramref name="path" /> is null or empty.</exception>
		/// <exception cref="InvalidDataException">When it is unable to automatically determine the field separator.</exception>
		public static string DetermineCSVSeparator (string path)
		{
			if (System.IO.Path.GetExtension(path).Equals(@".tsv", StringComparison.InvariantCultureIgnoreCase))
				return "\t";
			const int maxLinesToExamine = 3;
			var possibleSeparators = new [] { @",", @";", "\t", @"|", System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator }.Distinct();
			var results = possibleSeparators.Select(s => {
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
		
		/// <summary>
		/// Open a CSV file <paramref name="path" /> and get it's contents respresented in a <see cref="DataTable" />.
		/// </summary>
		/// <param name="path">The path to the CSV file.</param>
		/// <param name="separator">The field separator used in the CSV file.</param>
		/// <param name="containsHeaders">Specifies whether the CSV file contains a header row or not, which will be used to set the DataTable column headings.</param>
		/// <param name="inferTypes">Specifies whether to attempt to determine the type of each column based on it's contents.  Supports dates and numbers.</param>
		/// <param name="tolerateMalformedFile">Specifies whether to continue loading a file when some lines are malformed and contain too many fields.</param>
		/// <returns>A <see cref="DataTable" /> representing the contents of the CSV file.</returns>
		/// <exception cref="InvalidDataException">A line in the file is malformed and contains too many fields.</exception>
		public static DataTable Load(string path, string separator = null, bool containsHeaders = true, bool inferTypes = false, bool tolerateMalformedFile = false)
		{
			var csv = OpenCSV(path, separator);
			var dt = new DataTable(path);
			
			var iterator = csv.GetEnumerator();
			if (! iterator.MoveNext())
				return dt;
			
			var columnCounts = new Dictionary<string, int>();
			foreach (var header in iterator.Current)
			{
				if (containsHeaders)
				{
					if (!columnCounts.ContainsKey(header))
						columnCounts.Add(header, 1);
					else
						++columnCounts[header];
					dt.Columns.Add(header + (columnCounts[header] == 1 ? string.Empty : columnCounts[header].ToString()));
				}
				else
					dt.Columns.Add(string.Empty);
			}
			if (containsHeaders && !iterator.MoveNext())
				return dt;
			
			dt.BeginLoadData();
			ulong lineNo = 0;
			do {
				lineNo ++;
				if (tolerateMalformedFile) {
					// cope with invalid CSV files that contain more fields in some rows than there are columns defined
					for (int x = dt.Columns.Count; x < iterator.Current.Length; x++)
						dt.Columns.Add();
				} else if (dt.Columns.Count < iterator.Current.Length)
					throw new InvalidDataException(string.Format(@"Malformed line {0} in CSV '{1}' with separator '{2}'", lineNo, path, separator));
				
				// add the row to the data table
				dt.Rows.Add(iterator.Current);
			} while (iterator.MoveNext());
			
			dt.EndLoadData();
			
			if (inferTypes)
				dt.ConvertColumnsFromString(false);
			
			return dt;
		}
		
		public static string GetValueForCSV(object value, string separator)
		{
			const string quote = "\"";
			return ToOrFromString.GetValueBase(value, string.Empty, string.Empty, s => {
				s = s.Replace(Environment.NewLine, " ").Replace("\r", " ").Replace("\n", " "); // remove line breaks, replace with spaces
				
				// the following text qualification rules and quote doubling are based on recommendations in RFC 4180
				var qualify = s.Contains(quote) || s.EndsWith(" ") || s.EndsWith("\t") || (s.IndexOf(separator, StringComparison.InvariantCultureIgnoreCase) > -1); // qualify the text in quotes if it contains a quote, ends in whitespace, or contains the separator
				
				return qualify ? (quote + s.Replace(quote, quote + quote) + quote) : s; // to escape a quote, we double it up
			});
		}
		
		public static void Write(IEnumerable<string> headers, IEnumerable<IEnumerable<object>> values, TextWriter writer, string separator = "\t")
		{
			var lines = values.Select(row => string.Join(separator,
				row.Select(cell => CSV.GetValueForCSV(cell, separator))
			));
			
			if (headers != null && headers.Any())
				writer.WriteLine(string.Join(separator, headers));
			
			foreach (var line in lines)
				writer.WriteLine(line);
			
			writer.Flush();
		}
		
		/*public static IEnumerable<System.Dynamic.ExpandoObject> FromCSVEnumerable(IEnumerable<IEnumerable<string>> csv)
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
		}*/
	}
	
	public static class XML
	{
		/// <summary>
		/// Iterate through the specified <see cref="XmlReader" /> and build a <see cref="DataTable" /> from it.
		/// </summary>
		/// <param name="xr">The <see cref="XmlReader" /> to use to build the <see cref="DataTable" />.</param>
		/// <param name="row">The name of the XML element that represents a row.</param>
		/// <param name="hierarchySeparator">If null, will use short column names, as opposed to the path relative to the <paramref name="row" />.</param>
		/// <param name="includeAttributes">Include attributes in the <see cref="DataTable" />.</param>
		/// <param name="reverseHierarchy">Reverse the order of the column name hierarchy when not using short column names.</param> 
		/// <returns>A <see cref="DataTable" /> representing the XML.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="row" /> is null.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="xr" /> is null.</exception>
		public static DataTable ToDataTable(XmlReader xr, string row, string hierarchySeparator = null, bool includeAttributes = false, bool reverseHierarchy = false)
		{
			if (string.IsNullOrEmpty(row))
				throw new ArgumentNullException(nameof(row));
			if (xr == null)
				throw new ArgumentNullException(nameof(xr));
			
			bool shortCaptions = (hierarchySeparator == null);
			hierarchySeparator = hierarchySeparator ?? @"/";
			var stack = new Stack<string>();
			var dt = new DataTable();

			Action<string, DataRow> process = (name, relatedRow) =>
			{
				if (xr.HasValue && !string.IsNullOrWhiteSpace(xr.Value))
				{
					var level = stack.TakeWhile(v => !v.Equals(row));
					if (name != null)
						level = Enumerable.Concat(new[] { name }, level);
					if (reverseHierarchy)
						level = level.Reverse();
					var col = string.Join(hierarchySeparator, level);
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

			if (shortCaptions)
			{
				// NOTE: assumes none of the column names include the hierarchySeparator...
				var cols = dt.Columns.OfType<DataColumn>().Select(dc => new { Column = dc, FullName = dc.ColumnName, ShortName = dc.ColumnName.Split(new [] { hierarchySeparator }, StringSplitOptions.None).First() }).GroupBy(c => c.ShortName).Where(g => g.Count() == 1).Select(g => g.First());
				foreach (var col in cols)
					// x col.Column.Caption = col.ShortName;
					col.Column.ColumnName = col.ShortName;
			}
			return dt;
		}
	}
	
	public static class ToOrFromString
	{
		/// <summary>
		/// Determine if the input <see cref="String" /> <paramref name="value" /> is a valid number, date or boolean value.
		/// </summary>
		/// <param name="value">The <see cref="String" /> to attempt to convert.</param>
		/// <returns>The <see cref="String" /> <paramref name="value" /> if not possible to convert, otherwise a <see cref="Decimal" />, <see cref="DateTime" /> or <see cref="Boolean" />.</returns>
		public static object ConvertValueFromString(string value)
		{
			if (value == null)
				return null;
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
		
		internal static string GetValueBase(object value, string nullSubstitution, string byteArrayPrefix, Func<string, string> escapeIfNecessary)
		{
			if (value == null || value is DBNull)
				return nullSubstitution;
			
			Func<bool, string> toBoolString = b => escapeIfNecessary(b ? @"1" : @"0");
			Func<DateTime, string> toDateString = d => escapeIfNecessary(d.ToSortableDateTime());
			
			if (value is DateTime)
				return toDateString((DateTime)value);
			if (value is bool)
				return toBoolString((bool)value);
			if (value is Byte[])
				return escapeIfNecessary((byteArrayPrefix ?? string.Empty) + (new SoapHexBinary(value as Byte[]).ToString()));
			if (value is string)
				return escapeIfNecessary((string)value);
			
			return escapeIfNecessary(value.ToString());
		}
		
		
		public static string GetValueForSQL(object value)
		{
			return GetValueBase(value, @"null", @"0x", s => {
				if (value is decimal || value is int || value is long || value is float || value is bool || value is Byte[]) // if it is a numeric type or a varbinary, no need to escape it
					return s;
				else
					return @"'" + s.Replace(@"'", @"''") + @"'"; // escape the value
			});
		}
	}
}
