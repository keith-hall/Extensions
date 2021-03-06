using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Xml;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using OfficeOpenXml;

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
		
		/// <summary>
		/// Get the contents of the <see cref="DataTable" /> <paramref name="table" /> as a <see cref="String" /> in CSV format.
		/// </summary>
		/// <param name="table">The <see cref="DataTable" /> whose contents to write.</param>
		/// <param name="separator">The field separator to use in the CSV output.</param>
		/// <param name="includeColumnNamesAsHeader">Whether to include a header row with the column names from the <paramref name="table" /> in the CSV.</param>
		/// <returns>A <see cref="String" /> containing the data in CSV format.</returns>
		public static string WriteToCSV(this DataTable table, string separator = "\t", bool includeColumnNamesAsHeader = true)
		{
			var sw = new StringWriter();
			WriteToCSV(table, sw, separator, includeColumnNamesAsHeader);
			return sw.ToString();
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
		
		#region Excel
		public static ExcelRangeBase ToExcelSheet(this DataTable dataTable, ExcelWorksheet sheet, ExcelRangeBase start = null)
		{
			if (start == null)
				start = sheet.Cells[@"A1"];
			var range = start.LoadFromDataTable(dataTable, true, OfficeOpenXml.Table.TableStyles.Medium23);
			var table = sheet.Tables.Where(t => t.Address.ToString() == range.Address.ToString()).Single();

			// fix formatting of dates
			foreach (var col in dataTable.Columns.OfType<DataColumn>())
			{
				if (col.DataType == typeof(DateTime))
				{
					sheet.Column(col.Ordinal + range.Start.Column).Style.Numberformat.Format = @"YYYY-MM-dd" + (dataTable.Rows.OfType<DataRow>().Any(dr => !(dr[col] is DBNull) && ((DateTime)dr[col]).TimeOfDay.Ticks > 0) ? @" HH:mm:ss" : string.Empty);
				}
			}

			// freeze first row
			sheet.View.FreezePanes(range.Start.Row + 1, range.Start.Column);
			// expand column widths
			sheet.Cells[range.Address].AutoFitColumns();
			return range;
		}
		
		public static void ToExcelFile(this DataTable dataTable, string path, string sheetName = null)
		{
			sheetName = sheetName.NullIfEmpty() ?? dataTable.TableName.NullIfEmpty() ?? @"Sheet1"; // TODO: use base file name instead of Sheet1?
			
			using (var package = new ExcelPackage(new FileInfo(path)))
			{
				using (var sheet = package.Workbook.Worksheets.Add(sheetName))
				{
					ToExcelSheet(dataTable, sheet);
					package.Save();
				}
			}
		}
		#endregion
		
		/* example usage:
		```cs
		var a = @"Period	Card Name	Sum	Transaction Count
		2017-01	Visa Classic	1058.65	348
		2017-01	Visa Gold	761.00	210
		2017-01	Visa Gold	1.00	1
		2017-02	Visa Gold	675.12	78
		2017-02	Visa Classic	953.87	321
		2017-03	Other	12.99	1
		2017-03	Visa Classic	1157.41	401
		2017-03	Visa Gold	1243.56	245
		";
		using (var tr = new StringReader(a)) {
			var r = HallLibrary.Extensions.CSV.OpenCSV(tr, "\t");
			var dt = HallLibrary.Extensions.CSV.Load(r).Dump();
			HallLibrary.Extensions.DataTableExtensions.PivotData(dt, new[] { "Period" }, new[] { "Sum", "Transaction Count" }, (s, o1, o2) => decimal.Parse(o2.ToString()) + ((o1 == null || o1 is DBNull) ? (decimal)0.0 : decimal.Parse(o1.ToString()))).Dump();
		}
		*/
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
		/// <summary>
		/// Create a SQL "insert" statement for the data in the specified <paramref name="dt" />.
		/// </summary>
		/// <param name="dt">The <see cref="DataTable" />s whose data will be written as a SQL "insert" statement.</param>
		/// <param name="tableName">The name of the table to use in the insert statement.</param>
		/// <param name="createTable">Whether or not a "CREATE TABLE"/"DECLARE @"... "TABLE" statement should also be written.</param>
		/// <param name="writer">The <see cref="TextWriter" /> to output the insert statement(s) to.</param>
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
		
		public static string TableToSQLInsert(this DataTable dt, string tableName, bool createTable)
		{
			var sw = new StringWriter();
			TableToSQLInsert(dt, tableName, createTable, sw);
			return sw.ToString();
		}
		#endregion
	
		#region Data Table Filters
		/// <summary>
		/// Filter the specified <see cref="DataTable" /> <paramref name="table" /> into a new <see cref="DataTable" /> using the specified <paramref name="filter" />.
		/// </summary>
		/// <param name="table">The <see cref="DataTable" /> to filter.</param>
		/// <param name="filter">The filter expression.</param>
		/// <returns>A new <see cref="DataTable" /> containing the filtered rows.</returns>
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
		/// Open a CSV file at the specified <paramref name="path" />, automatically determining the field separator.
		/// </summary>
		/// <param name="path">The path to the CSV file.</param>
		/// <returns>An enumerable containing a <see cref="String" /> array all the fields in each row.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="path" /> is null or empty.</exception>
		/// <exception cref="InvalidDataException">When it is not possible to automatically determine the field separator.</exception>
		public static IEnumerable<string[]> OpenCSV(string path)
		{
			var separator = DetermineCSVSeparator(path);
			return OpenCSV(path, separator);
		}
		
		/// <summary>
		/// Open a CSV file at the specified <paramref name="path" /> using the specified field <paramref name="separator" />.
		/// </summary>
		/// <param name="path">The path to the CSV file.</param>
		/// <param name="separator">The field separator to use.</param>
		/// <returns>An enumerable containing a <see cref="String" /> array all the fields in each row.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="path" /> or <paramref name="separator" /> is null or empty.</exception>
		public static IEnumerable<string[]> OpenCSV(string path, string separator)
		{
			using (var reader = new StreamReader(path))
			{
				foreach (var row in OpenCSV(reader, separator))
				{
					yield return row;
				}
			}
		}
		
		/// <summary>
		/// Read the CSV file from the specified <paramref name="reader" /> using the specified field <paramref name="separator" />.
		/// </summary>
		/// <param name="reader">The <see cref="TextReader" /> responsible reading the CSV contents.</param>
		/// <param name="separator">The field separator to use.</param>
		/// <returns>An enumerable containing a <see cref="String" /> array all the fields in each row.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="reader" /> or <paramref name="separator" /> is null or empty.</exception>
		public static IEnumerable<string[]> OpenCSV(TextReader reader, string separator)
		{ //http://msdn.microsoft.com/en-us/library/microsoft.visualbasic.fileio.textfieldparser.aspx
			using (var tfp = new Microsoft.VisualBasic.FileIO.TextFieldParser(reader))
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
			
			using (var reader = new StreamReader(path))
			{
				return DetermineCSVSeparator(reader);
			}
		}
		
		/// <summary>
		/// Parse a few rows from the specified CSV to determine what field separator is used.
		/// Note: the reader's state will not be maintained.
		/// </summary>
		/// <param name="reader">The <see cref="TextReader" /> responsible reading the CSV contents.</param>
		/// <returns>The field separator used in the specified CSV file.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="TextReader" /> is null or empty.</exception>
		/// <exception cref="InvalidDataException">When it is unable to automatically determine the field separator.</exception>
		public static string DetermineCSVSeparator (TextReader reader)
		{
			const int maxLinesToExamine = 3;
			
			var possibleSeparators = new [] { "\t", @"|", System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator, @",", @";" }.Distinct();
			
			// buffer the lines to examine from the source reader
			var lines = new StringBuilder();
			foreach (var i in Enumerable.Range(0, maxLinesToExamine))
			{
				var line = reader.ReadLine();
				if (line == null)
					break;
				lines.AppendLine(line);
			}
			
			var results = possibleSeparators.Select(s => {
				List<int> c = null;
				try {
					c = OpenCSV(new StringReader(lines.ToString()), s).Take(maxLinesToExamine).Select(r => r.Length).Distinct().ToList();
				} catch (Microsoft.VisualBasic.FileIO.MalformedLineException) {
					// this is the wrong separator. We will try another one, if there are separators left in the list of possible separators.
				}
				return new { Separator = s, Contents = c };
			}).Where(r => r.Contents != null);
			var bestMatch = results.SingleOrDefault(r => !r.Contents.CountExceeds(1) && r.Contents.Single() > 1); // if there are multiple distinct values then it can't be the correct field separator, and if it only returns one field then it is also incorrect
			if (bestMatch == null) // if there are 0 or more than 1 result, we cannot be sure what the correct field separator is
				throw new InvalidDataException(@"Unable to automatically determine column separator");
			
			return bestMatch.Separator;
		}
		
		/// <summary>
		/// Open a CSV file <paramref name="path" /> and get it's contents represented in a <see cref="DataTable" />.
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
			var csv = separator == null ? OpenCSV(path) : OpenCSV(path, separator);
			return Load(csv, containsHeaders, inferTypes, tolerateMalformedFile, path);
		}
		
		/// <summary>
		/// Get the contents of the specified <paramref name="data" /> represented in a <see cref="DataTable" />.
		/// </summary>
		/// <param name="data">The data to represent in a <see cref="DataTable" />.</param>
		/// <param name="containsHeaders">Specifies whether the data contains a header row or not, which will be used to set the DataTable column headings.</param>
		/// <param name="inferTypes">Specifies whether to attempt to determine the type of each column based on it's contents.  Supports dates and numbers.</param>
		/// <param name="tolerateMalformedFile">Specifies whether to continue loading the data when some rows are malformed and contain too many fields.</param>
		/// <returns>A <see cref="DataTable" /> representing the contents of the given <paramref name="data" />.</returns>
		/// <exception cref="InvalidDataException">A row in the data is malformed and contains too many fields.</exception>
		public static DataTable Load(IEnumerable<string[]> data, bool containsHeaders = true, bool inferTypes = false, bool tolerateMalformedFile = false, string name = null)
		{
			var dt = new DataTable(name);
			
			var iterator = data.GetEnumerator();
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
					throw new InvalidDataException(string.Format(@"Malformed line {0} in data - expected {1} columns, found {2}", lineNo, dt.Columns.Count, iterator.Current.Length));
				
				// add the row to the data table
				dt.Rows.Add(iterator.Current);
			} while (iterator.MoveNext());
			
			dt.EndLoadData();
			
			if (inferTypes)
				dt.ConvertColumnsFromString(false);
			
			return dt;
		}
		
		/// <summary>
		/// Convert <see cref="Object" /> <paramref name="value" /> to a string that can be safely output in a CSV file.
		/// </summary>
		/// <param name="value">The object to convert into it's equivalent/safe representation in CSV.</param>
		/// <param name="separator">The field separator that will be used in the CSV, so that <paramref name="value" /> can be appropriately escaped.</param>
		/// <returns><paramref name="value" /> represented in CSV.</returns>
		public static string GetValueForCSV(object value, string separator, bool replaceLineBreaks = false)
		{
			const string quote = "\"";
			return ToOrFromString.GetValueBase(value, string.Empty, string.Empty, s => {
				if (replaceLineBreaks)
					s = s.Replace(Environment.NewLine, " ").Replace("\r", " ").Replace("\n", " "); // remove line breaks, replace with spaces
				
				// the following text qualification rules and quote doubling are based on recommendations in RFC 4180
				var qualify = s.Contains(quote) || s.EndsWith(" ") || s.EndsWith("\t") || s.StartsWith(" ") || s.StartsWith("\t") || (s.IndexOf("\n", StringComparison.InvariantCultureIgnoreCase) > -1) || (s.IndexOf(separator, StringComparison.InvariantCultureIgnoreCase) > -1); // qualify the text in quotes if it contains a quote, starts or ends in whitespace, or contains the separator or a newline
				
				return qualify ? (quote + s.Replace(quote, quote + quote) + quote) : s; // to escape a quote, we double it up
			});
		}
		
		/// <summary>
		/// Write the specified <paramref name="values" /> to a CSV file, with an optional line of column <paramref name="headers" />.
		/// </summary>
		/// <param name="headers">The optional column headings to output as the first line in the CSV output. Leave null or empty for no headers.</param>
		/// <param name="values">The lines of objects to convert into their equivalent/safe representation in CSV.</param>
		/// <param name="writer">The <see cref="TextWriter" /> to output the CSV to.</param>
		/// <param name="separator">The field separator to use in the CSV output.</param>
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
	
	/// <summary>
	/// Helpers for working with XML documents.
	/// </summary>
	public static class XML
	{
		/// <summary>
		/// Iterate through the specified <paramref name="xml" /> and build a <see cref="DataTable" /> from it.
		/// </summary>
		/// <param name="xml">The XML element in which to find rows.  Rows are taken from the first element in document order that contains multiple child elements.</param>
		/// <param name="hierarchySeparator">If null, will use short column names, as opposed to the path relative to the row.</param>
		/// <param name="includeAttributes">Include attributes in the <see cref="DataTable" />.</param>
		/// <param name="reverseHierarchy">Reverse the order of the column name hierarchy when not using short column names.</param> 
		/// <returns>A <see cref="DataTable" /> representing the XML.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="xml" /> is null.</exception>
		public static DataTable ToDataTable(XElement xml, string hierarchySeparator = null, bool includeAttributes = false, bool reverseHierarchy = false)
		{
			return ToDataTable(xml.DescendantsAndSelf().First(x => x.Elements().CountExceeds(1)).Elements(), hierarchySeparator, includeAttributes, reverseHierarchy);
		}
	
		/// <summary>
		/// Iterate through the specified <paramref name="rows" /> and build a <see cref="DataTable" /> from them.
		/// </summary>
		/// <param name="rows">The XML elements that each represent a row.</param>
		/// <param name="hierarchySeparator">If null, will use short column names, as opposed to the path relative to the row.</param>
		/// <param name="includeAttributes">Include attributes in the <see cref="DataTable" />.</param>
		/// <param name="reverseHierarchy">Reverse the order of the column name hierarchy when not using short column names.</param> 
		/// <returns>A <see cref="DataTable" /> representing the XML.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="rows" /> is null.</exception>
		public static DataTable ToDataTable(IEnumerable<XElement> rows, string hierarchySeparator = null, bool includeAttributes = false, bool reverseHierarchy = false)
		{
			if (rows == null || ! rows.Any())
				throw new ArgumentNullException(nameof(rows));
			
			bool shortCaptions = (hierarchySeparator == null);
			hierarchySeparator = hierarchySeparator ?? @"/";
			var stack = new Stack<string>();
			var rowsAllWithSameName = rows.Select(r => r.Name.LocalName).Distinct().CountEquals(1);
			var dt = new DataTable(rowsAllWithSameName ? rows.First().Name.LocalName : rows.First().Parent?.Name.LocalName);
	
			Action<string, DataRow, string> processValue = (name, dataRow, value) =>
			{
				var level = Enumerable.Concat(new[] { name }, stack);
				if (reverseHierarchy)
					level = level.Reverse();
				var col = string.Join(hierarchySeparator, level);
				if (!dt.Columns.Contains(col))
					dt.Columns.Add(col);
	
				dataRow.SetField(col, value);
			};
	
			Action<DataRow, XElement, bool> processElement = null;
			processElement = (dataRow, xe, addToStack) =>
			{
				if (includeAttributes && xe.HasAttributes)
				{
					if (addToStack)
						stack.Push(xe.Name.LocalName);
					foreach (var attr in xe.Attributes())
					{
						processValue("@" + attr.Name.LocalName, dataRow, attr.Value);
					}
					if (addToStack)
						stack.Pop();
				}
				if (!xe.HasElements)
				{
					processValue(xe.Name.LocalName, dataRow, xe.Value);
				}
				else
				{
					foreach (var element in xe.Elements())
					{
						if (addToStack)
							stack.Push(xe.Name.LocalName);
						processElement(dataRow, element, true);
						if (addToStack)
							stack.Pop();
					}
				}
			};
	
			foreach (var row in rows)
			{
				var dataRow = dt.NewRow();
				if (!rowsAllWithSameName)
					processValue("#row", dataRow, row.Name.LocalName);
				processElement(dataRow, row, false);
				dt.Rows.Add(dataRow);
			}
	
			if (shortCaptions)
			{
				// NOTE: assumes none of the column names include the hierarchySeparator...
				var cols = dt.Columns.OfType<DataColumn>().Select(dc => new { Column = dc, FullName = dc.ColumnName, ShortName = dc.ColumnName.Split(new[] { hierarchySeparator }, StringSplitOptions.None) }).GroupBy(c => reverseHierarchy ? c.ShortName.Last() : c.ShortName.First()).Where(g => g.CountEquals(1)).Select(g => new { Col = g.First(), ShortName = g.Key });
				foreach (var g in cols)
					g.Col.Column.ColumnName = g.ShortName;
			}
			return dt;
		}
	}
	
	/// <summary>
	/// Helpers for converting values to <see cref="String" /> or from <see cref="String" />.
	/// </summary>
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
		
		/// <summary>
		/// Convert <see cref="Object" /> <paramref name="value" /> to a string that can be used in a SQL statement.
		/// For example, strings are quoted and escaped to prevent SQL injection, dates are converted to an unambiguous format, byte arrays are represented as varbinary etc.
		/// </summary>
		/// <param name="value">The object to convert into it's equivalent representation in SQL.</param>
		/// <returns><paramref name="value" /> represented in SQL.</returns>
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
