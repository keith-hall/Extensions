<Query Kind="Program">
  <Output>DataGrids</Output>
  <Reference>&lt;RuntimeDirectory&gt;\System.Management.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.Install.dll</Reference>
  <Namespace>System.Management</Namespace>
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
		// TODO: prompt user for values?
		// use defaults
		machine = Environment.MachineName; // current machine
		since = DateTime.Now.AddDays(-1); // within a day (24 hours)
	#endif
	
	var conOpt = new ConnectionOptions();
	conOpt.Impersonation = ImpersonationLevel.Impersonate;
	conOpt.EnablePrivileges = true;
	
	var scope = new ManagementScope(String.Format(@"\\{0}\ROOT\CIMV2", machine), conOpt);
	
	scope.Connect();
	
	if (scope.IsConnected)
	{
		
		var query = new SelectQuery("Select * from Win32_NTLogEvent Where Logfile = 'Application' and TimeGenerated >='" + ManagementDateTimeConverter.ToDmtfDateTime(since) + "'");
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
