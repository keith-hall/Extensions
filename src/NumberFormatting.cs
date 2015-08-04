namespace HallLibrary.Extensions
{
	public static class NumberFormatting
	{
		private static readonly Regex _number = new Regex(@"^-?\d+(?:" + _defaultDecimalSeparatorForRegex + @"\d+)?$"); // TODO: replace dot with decimal separator from current culture
		private static readonly Regex _thousands = new Regex(@"(?<=\d)(?<!" + _defaultDecimalSeparatorForRegex + @"\d*)(?=(?:\d{3})+($|" + _defaultDecimalSeparatorForRegex + @"))"); // TODO: replace dot with decimal separator from current culture
		private const char _defaultThousandsSeparator = ',';
		private const string _defaultDecimalSeparatorForRegex = @"\.";
		
		public static string AddThousandsSeparators (string number, string thousandsSeparator = null) {
			if (_number.IsMatch(number)) {
				return _thousands.Replace(number, thousandsSeparator ?? _defaultThousandsSeparator.ToString());
			} else {
				return number;
			}
		}
	}
}
