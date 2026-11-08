using System;
using System.Diagnostics;

namespace BSCustomClevoController.Utility
{
	internal class DebugLog
	{
		public static void WriteDbgLog2Outlog(string log)
		{
			try
			{
				EventLog eventLog = new EventLog();
				eventLog.Source = "PowerBIOSServer_Out";
				eventLog.Log = "OutLog";
				log = "DebugFan:" + log;
				eventLog.WriteEntry(log);
			}
			catch
			{
			}
		}

		public static void SendOutlog_Event(string log)
		{
			try
			{
				EventLog eventLog = new EventLog();
				eventLog.Source = "PowerBIOSServer_Out";
				eventLog.Log = "OutLog";
				log = "clevofan^" + log;
				eventLog.WriteEntry(log);
			}
			catch
			{
			}
		}
	}
}
