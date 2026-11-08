using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BSCustomClevoController.Utility
{
	public class RWIni
	{
		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		public static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retStr, int bufferSize, string filePath);

		[DllImport("kernel32")]
		private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern int GetPrivateProfileSection(string lpAppName, byte[] lpszReturnBuffer, int bufferSize, string filePath);

		public RWIni()
		{
			this.path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
			this.lang = Global.RW_REG.Check_language();
		}

		public string Translation(string Text)
		{
			RWIni.GetPrivateProfileString(this.lang, Text, this.Default, this.outStr, this.Size, this.path + "\\mtlanguage.ini");
			return this.outStr.ToString();
		}

		public string Read_OEM(string section, string key)
		{
			RWIni.GetPrivateProfileString(section, key, this.Default, this.outStr, this.Size, this.path + "\\oem.ini");
			return this.outStr.ToString();
		}

		public string Read_Custom(string section, string key)
		{
			RWIni.GetPrivateProfileString(section, key, this.Default, this.outStr, this.Size, this.path + "\\Custom.ini");
			return this.outStr.ToString();
		}

		public string Read_Batery(string section, string key)
		{
			RWIni.GetPrivateProfileString(section, key, this.Default, this.outStr, this.Size, this.path + "\\Battery.ini");
			return this.outStr.ToString();
		}

		public List<string> Read_OEM_list(string section)
		{
			byte[] array = new byte[128];
			RWIni.GetPrivateProfileSection(section, array, 128, this.path + "\\oem.ini");
			string[] array2 = Encoding.Unicode.GetString(array).Trim(new char[1]).Split(new char[1]);
			bool flag = array2.Length == 0;
			List<string> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				bool flag2 = array2.Length == 1 && array2[0] == "";
				if (flag2)
				{
					result = null;
				}
				else
				{
					List<string> list = new List<string>();
					foreach (string text in array2)
					{
						list.Add(text.Substring(0, text.IndexOf("=")));
					}
					result = list;
				}
			}
			return result;
		}

		private string path;

		private StringBuilder outStr = new StringBuilder(255);

		private string Default = "null";

		private int Size = 255;

		private string lang = "en-US";
	}
}
