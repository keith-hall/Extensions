using System.Text.RegularExpressions;
using System;

namespace HallLibrary.Extensions
{
	public static class NumberFormatting
	{
		private static readonly Regex _number = new Regex(@"^-?\d+(?:" + _defaultDecimalSeparatorForRegex + @"\d+)?$"); // TODO: replace dot with decimal separator from current culture
		private static readonly Regex _thousands = new Regex(@"(?<=\d)(?<!" + _defaultDecimalSeparatorForRegex + @"\d*)(?=(?:\d{3})+($|" + _defaultDecimalSeparatorForRegex + @"))"); // TODO: replace dot with decimal separator from current culture
		private const char _defaultThousandsSeparator = ',';
		private const string _defaultDecimalSeparatorForRegex = @"\.";
		
		public static bool IsValidNumber (string value)
		{
			return _number.IsMatch(value);
		}
		
		public static string AddThousandsSeparators (string number, string thousandsSeparator = null)
		{
			if (!IsValidNumber(number))
				throw new ArgumentException(nameof(number), "String does not contain a valid number");
			return _thousands.Replace(number, thousandsSeparator ?? _defaultThousandsSeparator.ToString()); // TODO: replace comma with thousands separator from current culture
		
			/*
			// alternative implementation, without regex
			var digits = write.IndexOf(".", StringComparison.InvariantCultureIgnoreCase);
			if (digits == -1)
				digits = write.Length;

			for (var pos = digits - 3; pos > 0; pos -= 3)
			{
				write = write.Substring(0, pos) + thousandsSeparator + write.Substring(pos);
			}
			*/
		}
		/*
		// Store integer 182
		int decValue = 182;
		// Convert integer 182 as a hex in a string variable
		string hexValue = decValue.ToString("X"); // doesn't add 0x prefix
		// Convert the hex string back to the number
		int decAgain = int.Parse(hexValue, System.Globalization.NumberStyles.HexNumber); // doesn't cope with 0x prefix
		*/
	}
}
