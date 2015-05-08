<Query Kind="Statements">
  <NuGetReference>HallLibrary.Extensions</NuGetReference>
  <Reference>test.xml</Reference>
  <Reference>example.html</Reference>
  <Namespace>HallLibrary.Extensions</Namespace>
</Query>

#region Enumerable CountExceeds
	// create an enumerable where the 5th item will cause a DivideByZero Exception
	var src = Enumerable.Range(1, 4).Concat(new [] { 0 }).Select(s => 1 / s);
	// prove that it contains more than 3 items
	Debug.Assert(src.CountExceeds(3));
	// prove that using the normal Count method will fail
	try {
		if (src.Count() > 3)
			Debug.Fail(@"The above should cause a DivideByZero Exception so this line should never be reached.");
		else
			Debug.Fail(@"Count is greater than 3 so this line should never be reached.");
	} catch (DivideByZeroException) {
		
	} catch (Exception) {
		Debug.Fail(@"The above should cause a DivideByZero Exception so this line should never be reached.");
	}
	
	// demonstrate that it must exceed the number of items specified
	src = Enumerable.Range(1, 2);
	Debug.Assert(!src.CountExceeds(2));
#endregion


#region String
	// demonstrating count occurrences
	var text = @"hello world";
	var substring = @"l";
	string.Format("\"{0}\" contains {1} occurrences of \"{2}\"", text, text.CountOccurrences(substring), substring).Dump(@"count occurrences");
	
	// demonstrating simple lightweight html extractor
	text = File.ReadAllText(Util.GetFullPath(@"example.html"));
	var list = text.TextBetween(@"<ul>", @"</ul>");
	var listItems = list.AllTextBetween(@"<li>", @"</li>");
	listItems.Dump(@"contents of li tags");
	
	// please see tests on GitHub for more examples
#endregion

#region DataTable From XML
	HallLibrary.Extensions.DataTableExtensions.ReadXML(
		XmlReader.Create(Util.GetFullPath(@"test.xml")),
		@"User",
		false /* don't shorten column names*/,
		true /* include attributes */
	).Dump(@"DataTable from XML");
#endregion
