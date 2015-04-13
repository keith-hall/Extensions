public static class DataTableExtensions {
	public static DataTable CSVToDataTable (string path, string separator) {
		var csv = OpenCSV(path, separator).ToList();
		var dt = new DataTable(path);
		foreach (var header in csv.First())
			dt.Columns.Add(header);
		foreach (var values in csv.Skip(1))
			dt.Rows.Add(values);
		return dt;
	}
	
	public static DataTable ToDataTable (this IEnumerable<IDictionary<string, object>> csv, string name = null) {
		var result = new DataTable(name);
		if (! csv.Any())
			return result;
		
		var csvList = csv.ToList();
		foreach (var k in csvList.First() as IDictionary<string, object>).Select(e => e.Key))
			result.Columns.Add(k.ToString())
		
		csvList.ForEach(r => result.Rows.Add((r as IDictionary<string, object>).Values.ToArray()));
		
		return result;
	}
	
	public static IEnumerable<System.Dynamic.ExpandoObject> FromCSVEnumerable (IEnumerable<IEnumerable<string>> csv) {
		var headers = csv.First().ToArray();
		
		foreach (var row in csv.Skip(1))
		{
			var obj = new System.Dynamic.ExpandoObject();
			var ret = obj as IDictionary<string,object>;
			
			//for (int i = 0; i < headers.Length; i++)
			//	ret.Add(headers[i], row.ElementAt(i));
			var zipped = headers.Zip(row, (header, value) => new KeyValuePair<string, object>(header, value));
			foreach (var kvp in zipped)
				ret.Add(kvp);
			
			yield return obj;
		}
	}
	
	public static IEnumerable<string[]> OpenCSV (string path, string separator) { //http://msdn.microsoft.com/en-us/library/microsoft.visualbasic.fileio.textfieldparser.aspx
		using (var tfp = new Microsoft.VisualBasic.FileIO.TextFieldParser(path)) {
			try {
				tfp.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
				tfp.SetDelimiters(separator);
				
				while (! tfp.EndOfData)
					yield return tfp.ReadFields();
			} finally {
				tfp.Close();
			}
		}
	}
	
	public static string TableToSQLInsert (this DataTable dt, string tableName, bool createTable) {
		var sql = "insert into " + tableName + " (" +
			string.Join(", ", dt.Columns.OfType<DataColumn>().Select(
				col => "[" + col.ColumnName + "]"
			)) + ") values\r\n";
		float tmp;
		sql += " (" + string.Join(")\r\n,(", dt.Rows.OfType<DataRow>().Select(
			row => string.Join(", ", dt.Columns.OfType<DataColumn>().Select(
				col => (row[col].ToString().Length < 7 && float.TryParse(row[col].ToString(), out tmp)) ? row[col].ToString() : ("'" + row[col].ToString().Replace("'", "''") + "'") // determine if number or not
			)))
		) + ")";
		
		if (createTable) {
			sql = (table.StartsWith("@") ? "DECLARE " : "CREATE TABLE ") + tableName + (tableName.StartsWith("@") ? " TABLE " : string.Empty) + " (" + string.Join(
				", ",
				dt.Columns.OfType<DataColumn>().Select(dc => "[" + dc.ColumnName + "] NVARCHAR(" + dt.Rows.OfType<DataRow>().Max(dr => dr[dc].ToString().Length).ToString() + ")")
				) + ")\r\n" + sql;
		}
		
		return sql;
	}
	
	public static DataTable Filter (this DataTable table, string filter) {
		return table.Filter(table.Select(filter));
	}
	
	public static DataTable Filter (this DataTable table, IEnumerable<DataRow> rows) {
		var filtered = table.Clone();
		foreach (var row in rows)
			filtered.Rows.Add(row.ItemArray);
		return filtered;
	}
	
	public static DataTable ReadXML (XmlReader xr, string row, bool shortNames, bool includeAttributes) {
		var stack = new Stack<string>();
		var dt = new DataTable();
		DataRow currentRow = null;
		
		Action process = () => {
			if (xr.HasValue && ! string.IsNullOrWhiteSpace(xr.Value) && xr.NodeType != XmlNodeType.Comment) {
				var col = string.Join("/", stack);
				if (! dt.Columns.Contains(col))
					dt.Columns.Add(col);
				
				if (currentRow != null)
					currentRow.SetField(col, xr.Value);
			}
			if (xr.NodeType == XmlNodeType.EndElement ||
				(xr.NodeType == XmlNodeType.Element && xr.IsEmptyElement) ||
				xr.NodeType == XmlNodeType.Attribute) {
				stack.Pop();
				if (xr.NodeType == XmlNodeType.EndElement && xr.Name == row)
					currentRow = null;
			}
		};
		
		while (xr.Read()) {
			if (xr.NodeType == XmlNodeType.Element) {
				if (String.IsNullOrEmpty(dt.TableName))
					dt.TableName = xr.Name;
				
				stack.Push(xr.Name);
				if (xr.Name == row) {
					currentRow = dt.NewRow();
					dt.Rows.Add(currentRow);
				}
			}
			if (xr.HasAttributes && includeAttributes) {
				xr.MoveToFirstAttribute();
				do {
					stack.Push(stack.Peek() + "@" + xr.Name);
					process();
				} while (xr.MoveToNextAttribute());
				xr.MoveToElement();
			}
			process();
		}
		xr.Close();
		
		if (shortNames) {
			var cols = dt.Columns.OfType<DataColumn>().Select(dc => new { Column = dc, FullName = dc.ColumnName, ShortName = dc.ColumnName.Split('/').First() }).GroupBy(c => c.ShortName).Where(g => g.Count() == 1).Select (g => g.First());
			foreach (var col in cols)
				//try {
					col.Column.ColumnName = col.ShortName;
				//} catch (DuplicateNameException) { // TODO: instead, only rename the columns that won't clash
				//}
		}
		return dt;
	}
}
