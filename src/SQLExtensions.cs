// code taken from: https://jonnybekkum.wordpress.com/2013/03/02/linqpad-dumpasinsert-extension/

public static class SQLExtensions {
	public static IEnumerable<T> DumpAsInsert<T>(this IEnumerable<T> data) where T:class
	{
		return DumpAsInsert(data, null);
	}

	public static IEnumerable<T> DumpAsInsert<T>(this IEnumerable<T> data, string tableName) where T:class
	{
		return DumpAsInsert(data, tableName, string.Empty);      
	}

	public static IEnumerable<T> DumpAsInsert<T>(this IEnumerable<T> data, string tableName, string hideColumn) where T:class
	{
		return DumpAsInsert(data, tableName, new string[] { hideColumn });
	}

	public static IEnumerable<T> DumpAsInsert<T>(this IEnumerable<T> data, string tableName, string[] hideColumns) where T:class
	{
		var firstItem = data.FirstOrDefault();
		if (firstItem == null) string.Empty.Dump();
		if (hideColumns == null) hideColumns = new [] { string.Empty };
		
		if (tableName == null)
			tableName = firstItem.GetType().Name;

		var formatProvider = GetSqlTextFormatInfo();
		var result = new StringBuilder();
		var members = new List<MemberInfo>();
		//var iterate = CheckIfAnonymousType(firstItem.GetType()) ? firstItem.GetType().GetProperties() : firstItem.GetType().GetFields();
		//members.AddRange(iterate.Where(p => !hideColumns.Contains(p.Name)));
		if (CheckIfAnonymousType(firstItem.GetType()))
			members.AddRange(firstItem.GetType().GetProperties().Where(p => !hideColumns.Contains(p.Name)));
		else
			members.AddRange(firstItem.GetType().GetFields().Where(p => !hideColumns.Contains(p.Name)));
		
		var stmt = string.Format("INSERT INTO [{0}] ({1})\nVALUES (", tableName, string.Join(", ", members.Select(p => string.Format("[{0}]", p.Name)).ToArray()));

		foreach (var item in data)
		{
			result.Append(stmt);

			var first = true;
			foreach (var col in members)
			{
				if (!first) {
					result.Append(",");
					first = false;
                }
				result.Append(GetFieldValue(formatProvider, col, item));
			}
			result.AppendLine(");");
		}
		
		result.ToString().Dump();
		
		return data;
	}

	public static string GetFieldValue(IFormatProvider formatProvider, MemberInfo field, object row)
	{
		object value;
		Type fieldType;
		if (field is FieldInfo)
		{
			value = ((FieldInfo)field).GetValue(row);
			fieldType = ((FieldInfo) field).FieldType;
		}
		else
		{
			value = ((PropertyInfo)field).GetValue(row, null);
			fieldType = ((PropertyInfo)field).PropertyType;
		}
		if (value == null) return "NULL";
		
		if (fieldType == typeof(bool))
			return (bool) value ? "1" : "0";
		
		if (fieldType == typeof(System.String))
			return "'" + value.ToString().Replace("'", "''") + "'";
		else if (fieldType == typeof(DateTime) || fieldType == typeof(DateTime?))
			return "convert(datetime, '" + ((DateTime) value).ToString("yyyy-MM-dd HH:mm:ssss.fffffff") + "', 120)";
		else if (fieldType == typeof(System.Data.Linq.Binary))
			return "NULL";
		else if (fieldType == typeof(XElement))
			return "'" + ((XElement)value).Value.Replace("'", "''") + "'";
		else
			return string.Format(formatProvider, "{0}", value);
	}

	private static System.Globalization.NumberFormatInfo GetSqlTextFormatInfo() 
	{
		return new System.Globalization.NumberFormatInfo()
		{
			CurrencyDecimalSeparator = ".",
			CurrencyGroupSeparator = string.Empty,
			NumberDecimalSeparator = ".",
			NumberGroupSeparator = string.Empty,
			PercentDecimalSeparator = ".",
			PercentGroupSeparator = string.Empty,
		};
	}
	
	private static bool CheckIfAnonymousType(Type type)
	{
		if (type == null)
			throw new ArgumentNullException("type");
		
		// HACK: The only way to detect anonymous types right now.
		return Attribute.IsDefined(type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)
			&& type.IsGenericType && type.Name.Contains("AnonymousType")
			&& (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
			&& (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
	}
}
