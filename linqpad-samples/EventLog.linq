<Query Kind="Program">
  <Output>DataGrids</Output>
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Management.dll</Reference>
  <NuGetReference>HallLibrary.Extensions</NuGetReference>
  <Namespace>HallLibrary.Extensions</Namespace>
  <Namespace>System.Management</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
</Query>

void Main(string[] args)
{
	string machine;
	DateTime since;
	
	#if CMD
		// lprun
		// TODO: read command line arguments
	#else
		// LINQPad GUI
		machine = Environment.MachineName;
		since = DateTime.Now.AddDays(-1).Date; // default to since yesterday midnight
		var prompt = new InteractivePrompt();
		prompt.AddCached("Machine", machine, newValue => machine = newValue);
		var datepicker = (DateTimePicker)prompt.AddCached("Since", since, newValue => since = newValue);
		datepicker.CustomFormat = ControlFactory.GetUniversalDateFormat();
		datepicker.Format = DateTimePickerFormat.Custom;
		datepicker.MaxDate = DateTime.Now;
		if (prompt.Prompt() != DialogResult.OK) {
			"Query Cancelled!".Dump();
			return;
		}
		if (string.IsNullOrEmpty(machine))
			machine = "localhost";
	#endif
	
	var conOpt = new ConnectionOptions();
	conOpt.Impersonation = ImpersonationLevel.Impersonate;
	conOpt.EnablePrivileges = true;
	
	var scope = new ManagementScope(String.Format(@"\\{0}\ROOT\CIMV2", machine), conOpt);
	
	scope.Connect();
	
	if (scope.IsConnected)
	{
		var query = new SelectQuery("Select * From Win32_NTLogEvent Where Logfile = 'Application' and TimeGenerated >= '" + ManagementDateTimeConverter.ToDmtfDateTime(since) + "'");
		var searcher = new ManagementObjectSearcher(scope, query);
		var logs = searcher.Get().OfType<ManagementObject>();
		
		Func<ManagementObject, string, object> FromLog = (log, property) => log.Properties[property].Value;
		logs.Select (l => new {
			When = ManagementDateTimeConverter.ToDateTime(FromLog(l, "TimeWritten").ToString()),
			SourceName = FromLog(l, "SourceName"),
			Message = FromLog(l, "Message")
			//ComputerName = FromLog(l, "ComputerName")
			//Category = FromLog(l, "Category")
			//User = FromLog(l, "User")
		}).Dump();
	}
}
/*
// Define other methods and classes here
public static string GetInput (string question, string defaultValue, Func<string, bool> validate = null) {
	var cached = AppDomain.CurrentDomain.GetData(question);
	
	var value = Interaction.InputBox(question, "Query", (string)cached ?? defaultValue);
	if (validate == null || validate(value))
		AppDomain.CurrentDomain.SetData(question, value);
	else if (validate != null)
		throw new InvalidDataException();
	
	return value;
}
*/
