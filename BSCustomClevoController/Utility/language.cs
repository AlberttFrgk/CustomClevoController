using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BSCustomClevoController.Utility
{
	public class language
	{
		[DllImport("kernel32", CharSet = CharSet.Unicode)]
		public static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retStr, int bufferSize, string filePath);

		[DllImport("kernel32")]
		private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

		public language()
		{
			this.path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\mtlanguage.ini";
		}

		public string Translation(string Text)
		{
			language.GetPrivateProfileString(this.lang, Text, this.Default, this.outStr, this.Size, this.path);
			return this.outStr.ToString();
		}

		public void Load_language()
		{
			this.lang = this.Check_language();
		}

		public string Check_language()
		{
			byte[] appdata = Global.insyde.GetAPPData(1, 2, 1);
			bool flag = appdata[0] == 1;
			string result;
			if (flag)
			{
				result = "en-us";
			}
			else
			{
				bool flag2 = appdata[0] == 3;
				if (flag2)
				{
					result = "es";
				}
				else
				{
					bool flag3 = appdata[0] == 4;
					if (flag3)
					{
						result = "de";
					}
					else
					{
						bool flag4 = appdata[0] == 5;
						if (flag4)
						{
							result = "it";
						}
						else
						{
							bool flag5 = appdata[0] == 6;
							if (flag5)
							{
								bool flag6 = Global.support_bit.CustomerID == 5;
								if (flag6)
								{
									result = "ja-ep";
								}
								else
								{
									result = "ja";
								}
							}
							else
							{
								bool flag7 = appdata[0] == 7;
								if (flag7)
								{
									result = "ko";
								}
								else
								{
									bool flag8 = appdata[0] == 8;
									if (flag8)
									{
										result = "zh-cn";
									}
									else
									{
										bool flag9 = appdata[0] == 9;
										if (flag9)
										{
											result = "zh-tw";
										}
										else
										{
											bool flag10 = appdata[0] == 10;
											if (flag10)
											{
												result = "fr-fr";
											}
											else
											{
												bool flag11 = appdata[0] == 11;
												if (flag11)
												{
													result = "fr-ca";
												}
												else
												{
													bool flag12 = appdata[0] == 12;
													if (flag12)
													{
														result = "pt-br";
													}
													else
													{
														bool flag13 = appdata[0] == 13;
														if (flag13)
														{
															result = "pt-pt";
														}
														else
														{
															bool flag14 = appdata[0] == 14;
															if (flag14)
															{
																result = "tr-tr";
															}
															else
															{
																bool flag15 = appdata[0] == 15;
																if (flag15)
																{
																	result = "th-th";
																}
																else
																{
																	bool flag16 = appdata[0] == 16;
																	if (flag16)
																	{
																		result = "vi-vn";
																	}
																	else
																	{
																		bool flag17 = appdata[0] == 17;
																		if (flag17)
																		{
																			result = "ru";
																		}
																		else
																		{
																			bool flag18 = appdata[0] == 18;
																			if (flag18)
																			{
																				result = "ja-ep";
																			}
																			else
																			{
																				result = "en-us";
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
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
