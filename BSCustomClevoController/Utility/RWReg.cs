using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace BSCustomClevoController.Utility
{
	public class RWReg
	{
		[DllImport("kernel32.dll")]
		private static extern int GetPrivateProfileSection(string lpAppName, byte[] lpszReturnBuffer, int nSize, string lpFileName);

		public void Init()
		{
		}

		public int window_FullSrceen
		{
			get
			{
				string text = Convert.ToString(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter3.0\\Fan\\", "window_FullSrceen", "null"));
				bool flag = text == "null";
				int result;
				if (flag)
				{
					result = 1;
				}
				else
				{
					result = (int)Convert.ToInt16(text);
				}
				return result;
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter3.0\\Fan\\", "window_FullSrceen", value.ToString());
			}
		}

		public int window_Color
		{
			get
			{
				string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\UI\\", "window_Color", "null").ToString();
				bool flag = text == "null";
				int result;
				if (flag)
				{
					result = 10617087;
				}
				else
				{
					result = Convert.ToInt32(text);
				}
				return result;
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\UI\\", "window_Color", value.ToString());
			}
		}

		public int First_run
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter3.0\\Fan\\", "First_run", 0));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter3.0\\Fan\\", "First_run", value, RegistryValueKind.DWord);
			}
		}

		public string ECVersion
		{
			get
			{
				return Convert.ToString(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter3.0\\", "ECVersion", "1."));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter3.0\\", "ECVersion", value, RegistryValueKind.String);
			}
		}

		public int window_Width
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter3.0\\Fan\\", "window_Width", 0));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter3.0\\Fan\\", "window_Width", value, RegistryValueKind.DWord);
			}
		}

		public int window_Height
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter3.0\\Fan\\", "window_Height", 0));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter3.0\\Fan\\", "window_Height", value, RegistryValueKind.DWord);
			}
		}

		public int UAC_Behavior
		{
			get
			{
				string text = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "ConsentPromptBehaviorAdmin", "5").ToString();
				bool flag = text == "null";
				int result;
				if (flag)
				{
					result = 5;
				}
				else
				{
					result = Convert.ToInt32(text);
				}
				return result;
			}
			set
			{
				Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "ConsentPromptBehaviorAdmin", value, RegistryValueKind.DWord);
			}
		}

		public int Support_Page_SystemMonitor()
		{
			return Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\UI\\", "Support_Page_SystemMonitor", "0"));
		}

		public int Support_Page_LEDDevice()
		{
			return Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\UI\\", "Support_Page_LEDDevice", "0"));
		}

		public string Check_language()
		{
			string text = Convert.ToString(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter3.0\\Fan\\", "language", ""));
			bool flag = text == "";
			if (flag)
			{
				text = CultureInfo.CurrentCulture.Name.ToLower();
				this.WriteLanguage(text);
			}
			bool flag2 = text.Contains("en");
			if (flag2)
			{
				bool flag3 = text == "en-us";
				if (flag3)
				{
					Global.Flexikey_LED_Language = 0;
				}
				else
				{
					Global.Flexikey_LED_Language = 1;
				}
				text = "en-us";
			}
			else
			{
				bool flag4 = text.Contains("es");
				if (flag4)
				{
					text = "es";
					Global.Flexikey_LED_Language = 1;
				}
				else
				{
					bool flag5 = text.Contains("de");
					if (flag5)
					{
						text = "de";
						Global.Flexikey_LED_Language = 1;
					}
					else
					{
						bool flag6 = text.Contains("it");
						if (flag6)
						{
							text = "it";
							Global.Flexikey_LED_Language = 1;
						}
						else
						{
							bool flag7 = text.Contains("ja");
							if (flag7)
							{
								text = "ja";
								Global.Flexikey_LED_Language = 2;
							}
							else
							{
								bool flag8 = text.Contains("ko");
								if (flag8)
								{
									text = "ko";
									Global.Flexikey_LED_Language = 0;
								}
								else
								{
									bool flag9 = text.Contains("zh-cn");
									if (flag9)
									{
										text = "zh-cn";
										Global.Flexikey_LED_Language = 0;
									}
									else
									{
										bool flag10 = text.Contains("zh-tw");
										if (flag10)
										{
											text = "zh-tw";
											Global.Flexikey_LED_Language = 0;
										}
										else
										{
											bool flag11 = text.Contains("fr-fr") || text.Contains("zh-fr");
											if (flag11)
											{
												text = "fr-fr";
												Global.Flexikey_LED_Language = 1;
											}
											else
											{
												bool flag12 = text.Contains("fr-ca") || text.Contains("zh-ca");
												if (flag12)
												{
													text = "fr-ca";
													Global.Flexikey_LED_Language = 1;
												}
												else
												{
													bool flag13 = text.Contains("pt-br") || text.Contains("zh-br");
													if (flag13)
													{
														text = "pt-br";
														Global.Flexikey_LED_Language = 3;
													}
													else
													{
														bool flag14 = text.Contains("pt-pt") || text.Contains("zh-pt");
														if (flag14)
														{
															text = "pt-pt";
															Global.Flexikey_LED_Language = 1;
														}
														else
														{
															bool flag15 = text.Contains("tr-tr") || text.Contains("zh-tr");
															if (flag15)
															{
																text = "tr-tr";
																Global.Flexikey_LED_Language = 1;
															}
															else
															{
																bool flag16 = text.Contains("th-th") || text.Contains("zh-th");
																if (flag16)
																{
																	text = "th-th";
																	Global.Flexikey_LED_Language = 0;
																}
																else
																{
																	bool flag17 = text.Contains("vi-vn") || text.Contains("zh-vn");
																	if (flag17)
																	{
																		text = "vi-vn";
																		Global.Flexikey_LED_Language = 0;
																	}
																	else
																	{
																		bool flag18 = text.Contains("ru");
																		if (flag18)
																		{
																			text = "ru";
																			Global.Flexikey_LED_Language = 1;
																		}
																		else
																		{
																			text = "en-us";
																			Global.Flexikey_LED_Language = 0;
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
			return text;
		}

		public int KB_language()
		{
			return Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\UI\\", "KB_language", "0"));
		}

		private void WriteLanguage(string strLang)
		{
			Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter3.0\\Fan\\", "language", strLang);
		}

		public int GPUOC_Freq
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\GPU", "GPUOC_Freq", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\GPU", "GPUOC_Freq", value, RegistryValueKind.DWord);
			}
		}

		public int GPUOC2_Freq
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\GPU", "GPUOC2_Freq", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\GPU", "GPUOC2_Freq", value, RegistryValueKind.DWord);
			}
		}

		public int GPUMOC_Freq
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\GPU", "GPUMOC_Freq", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\GPU", "GPUMOC_Freq", value, RegistryValueKind.DWord);
			}
		}

		public int GPUMOC2_Freq
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\GPU", "GPUMOC2_Freq", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\GPU", "GPUMOC2_Freq", value, RegistryValueKind.DWord);
			}
		}

		public int GPU_CoreINC_MAX()
		{
			return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\GPU", "GPUOCMax", "0"));
		}

		public int GPU_CoreINC_MIN()
		{
			return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\GPU", "GPUOCMin", "0"));
		}

		public int GPU_VRAMINC_MAX()
		{
			return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\GPU", "MEMOCMax", "0"));
		}

		public int GPU_VRAMINC_MIN()
		{
			return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\GPU", "MEMOCMin", "0"));
		}

		public int WMI_6()
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\WMI\\", "WMI_6", "0").ToString();
			return Convert.ToInt32(value);
		}

		public int WMI_8()
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\WMI\\", "WMI_8", "0").ToString();
			return Convert.ToInt32(value);
		}

		public int WMI_69()
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\WMI\\", "WMI_69", "0").ToString();
			return Convert.ToInt32(value);
		}

		public int WMI_70()
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\WMI\\", "WMI_70", "0").ToString();
			return Convert.ToInt32(value);
		}

		public int WMI_122()
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\WMI\\", "WMI_122", "0").ToString();
			return Convert.ToInt32(value);
		}

		public int Get_GPUSwitchStatus()
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\WMI\\", "WMI_84", "0").ToString();
			return (int)Convert.ToInt16(value);
		}

		public int Get_TP()
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\WMI\\", "WMI_42", "0").ToString();
			return (int)Convert.ToInt16(value);
		}

		public int Get_CCD()
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\WMI\\", "WMI_34", "0").ToString();
			return (int)Convert.ToInt16(value);
		}

		public int Get_LeftWinKey()
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0", "LeftWinKey", "1").ToString();
			return Convert.ToInt32(value);
		}

		public bool Get_DisableAirplane()
		{
			return Convert.ToBoolean(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0", "Disable_Airplane", null));
		}

		public bool Get_SupportDocking()
		{
			return Convert.ToBoolean(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0", "SupportDocking", null));
		}

		public int Get_DockingStatus()
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0", "DockingStatus", "0").ToString();
			return Convert.ToInt32(value);
		}

		public int Get_Fan_Table_duty(int FanIndex, int DutyIndex)
		{
			string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC\\Fan" + FanIndex.ToString(), "D" + DutyIndex.ToString(), "null").ToString();
			bool flag = text == "null";
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				result = Convert.ToInt32(text);
			}
			return result;
		}

		public void Set_Fan_Table_duty(int FanIndex, int DutyIndex, int value)
		{
			Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC\\Fan" + FanIndex.ToString(), "D" + DutyIndex.ToString(), value, RegistryValueKind.DWord);
		}

		public int Get_Fan_Table_temp(int FanIndex, int TempIndex)
		{
			string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC\\Fan" + FanIndex.ToString(), "T" + TempIndex.ToString(), "null").ToString();
			bool flag = text == "null";
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				result = Convert.ToInt32(text);
			}
			return result;
		}

		public void Set_Fan_Table_temp(int FanIndex, int TempIndex, int value)
		{
			Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC\\Fan" + FanIndex.ToString(), "T" + TempIndex.ToString(), value, RegistryValueKind.DWord);
		}

		public int Get_Fan_Table_duty_default(int FanIndex, int DutyIndex)
		{
			string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC\\Fan" + FanIndex.ToString(), "Default_D" + DutyIndex.ToString(), "null").ToString();
			bool flag = text == "null";
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				result = Convert.ToInt32(text);
			}
			return result;
		}

		public int Get_Fan_Table_temp_default(int FanIndex, int TempIndex)
		{
			string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC\\Fan" + FanIndex.ToString(), "Default_T" + TempIndex.ToString(), "null").ToString();
			bool flag = text == "null";
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				result = Convert.ToInt32(text);
			}
			return result;
		}

		public int CPUFan_duty()
		{
			string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC\\Fan1", "duty", "0").ToString();
			bool flag = text == "null";
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				result = Convert.ToInt32(text);
			}
			return result;
		}

		public int GPUFan_duty()
		{
			string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC\\Fan2", "duty", "0").ToString();
			bool flag = text == "null";
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				result = Convert.ToInt32(text);
			}
			return result;
		}

		public int GPUFan2_duty()
		{
			string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC\\Fan3", "duty", "0").ToString();
			bool flag = text == "null";
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				result = Convert.ToInt32(text);
			}
			return result;
		}

		public int CPUFan_rpm()
		{
			string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC\\Fan1", "rpm", "0").ToString();
			bool flag = text == "null";
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				result = Convert.ToInt32(text);
			}
			return result;
		}

		public int GPUFan_rpm()
		{
			string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC\\Fan2", "rpm", "0").ToString();
			bool flag = text == "null";
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				result = Convert.ToInt32(text);
			}
			return result;
		}

		public int GPUFan2_rpm()
		{
			string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC\\Fan3", "rpm", "0").ToString();
			bool flag = text == "null";
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				result = Convert.ToInt32(text);
			}
			return result;
		}

		public int FanMode()
		{
			string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC", "FanMode", "0").ToString();
			bool flag = text == "null";
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				result = Convert.ToInt32(text);
			}
			return result;
		}

		public int FanOffset()
		{
			string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC", "FanOffset", "0").ToString();
			bool flag = text == "null";
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				result = Convert.ToInt32(text);
			}
			return result;
		}

		public int FanCount()
		{
			string text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC", "FanCount", "0").ToString();
			bool flag = text == "null";
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				result = Convert.ToInt32(text);
			}
			return result;
		}

		public byte FanOCMode
		{
			get
			{
				return Convert.ToByte(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC", "FanOCMode", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC", "FanOCMode", value, RegistryValueKind.DWord);
			}
		}

		public bool SupportFanLess
		{
			get
			{
				return Convert.ToBoolean(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0", "SupportFanLess", null));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0", "SupportFanLess", value);
			}
		}

		public bool SupportMaxqFan
		{
			get
			{
				return Convert.ToBoolean(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\EC\\", "SupportMaxQ_FAN", null));
			}
		}

		public void LoadCurrentMode()
		{
			int num = (int)Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\wmi", "WMI_96", null);
			bool flag = ((num >> 2) & 1) == 1;
			if (flag)
			{
				this.SupportNewMode = true;
			}
			else
			{
				this.SupportNewMode = false;
			}
			bool supportNewMode = this.SupportNewMode;
			if (supportNewMode)
			{
				this.CurrentMode2 = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\", "CurrentMode2", "entertainment").ToString();
			}
			this.CurrentMode = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\", "CurrentMode", "entertainment").ToString();
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\ControlCenter2.0\\" + this.CurrentMode, false);
			this.CCD = (int)registryKey.GetValue("CCD");
			this.TouchPad = (int)Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0", "TouchPad", null);
			this.AirplaneMode = (int)registryKey.GetValue("AirplaneMode");
			this.HeadPhone = (int)registryKey.GetValue("HeadPhone");
			this.KBSleepTimer = (int)registryKey.GetValue("KBSleepTimer");
			this.KBSleepTimerOn = (((long)this.KBSleepTimer & (long)(int.MinValue)).Equals(0L) ? 1 : 0);
			this.KBSleepTimer &= 268435455;
		}

		public int KBType()
		{
			return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED", "KBType", "0"));
		}

		public int PerkeySpeed
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED", "KBSpeed", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED", "KBSpeed", value, RegistryValueKind.DWord);
			}
		}

		public int KBStatus
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED", "KBStatus", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED", "KBStatus", value, RegistryValueKind.DWord);
			}
		}

		public int KBbrightness
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED", "KBbrightness", "3"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED", "KBbrightness", value, RegistryValueKind.DWord);
			}
		}

		public bool SupportSelectBootEffect()
		{
			int num = Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\", "SupportKBSelectBootEffect", "0"));
			bool flag = num == 0;
			return !flag;
		}

		public int SelectBootEffect()
		{
			return Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\", "KBSelectBootEffect", "0"));
		}

		public int KB_RGB_Mode
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED", "KB_RGB_Mode", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED", "KB_RGB_Mode", value, RegistryValueKind.DWord);
			}
		}

		public void Set_KB_RGB(string part, int R, int G, int B)
		{
			Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\" + part, "R", R, RegistryValueKind.DWord);
			Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\" + part, "G", G, RegistryValueKind.DWord);
			Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\" + part, "B", B, RegistryValueKind.DWord);
		}

		public int Get_KB_R(string part)
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED" + part, "R", "0").ToString();
			return Convert.ToInt32(value);
		}

		public int Get_KB_G(string part)
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED" + part, "G", "0").ToString();
			return Convert.ToInt32(value);
		}

		public int Get_KB_B(string part)
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED" + part, "B", "0").ToString();
			return Convert.ToInt32(value);
		}

		public void Set_Perkey_Static_RGB(string id, byte R, byte G, byte B)
		{
			int num = ((int)R << 16) + ((int)G << 8) + (int)B;
			Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\Static", id, num, RegistryValueKind.DWord);
		}

		public int Get_Perkey_Static_RGB(string id)
		{
			string value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\Static", id, "0").ToString();
			return Convert.ToInt32(value);
		}

		public int Perkey_EffectMode
		{
			get
			{
				return Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "EffectMode", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "EffectMode", value, RegistryValueKind.DWord);
			}
		}

		public int Perkey_BlinkStatus
		{
			get
			{
				return Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "BlinkStatus", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "BlinkStatus", value, RegistryValueKind.DWord);
			}
		}

		public byte Blink_R
		{
			get
			{
				return Convert.ToByte(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Blink_R", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Blink_R", value, RegistryValueKind.DWord);
			}
		}

		public byte Blink_G
		{
			get
			{
				return Convert.ToByte(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Blink_G", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Blink_G", value, RegistryValueKind.DWord);
			}
		}

		public byte Blink_B
		{
			get
			{
				return Convert.ToByte(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Blink_B", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Blink_B", value, RegistryValueKind.DWord);
			}
		}

		public int Perkey_BreathStatus
		{
			get
			{
				return Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "BreathStatus", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "BreathStatus", value, RegistryValueKind.DWord);
			}
		}

		public byte Breath_R
		{
			get
			{
				return Convert.ToByte(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Breath_R", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Breath_R", value, RegistryValueKind.DWord);
			}
		}

		public byte Breath_G
		{
			get
			{
				return Convert.ToByte(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Breath_G", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Breath_G", value, RegistryValueKind.DWord);
			}
		}

		public byte Breath_B
		{
			get
			{
				return Convert.ToByte(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Breath_B", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Breath_B", value, RegistryValueKind.DWord);
			}
		}

		public int Perkey_RippleStatus
		{
			get
			{
				return Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "RippleStatus", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "RippleStatus", value, RegistryValueKind.DWord);
			}
		}

		public byte Ripple_R
		{
			get
			{
				return Convert.ToByte(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Ripple_R", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Ripple_R", value, RegistryValueKind.DWord);
			}
		}

		public byte Ripple_G
		{
			get
			{
				return Convert.ToByte(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Ripple_G", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Ripple_G", value, RegistryValueKind.DWord);
			}
		}

		public byte Ripple_B
		{
			get
			{
				return Convert.ToByte(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Ripple_B", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "Ripple_B", value, RegistryValueKind.DWord);
			}
		}

		public int Perkey_StaticStatus
		{
			get
			{
				return Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "StaticStatus", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED\\PKB\\", "StaticStatus", value, RegistryValueKind.DWord);
			}
		}

		public int PerKey_Type()
		{
			return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\LED", "PerKey_Type", "0"));
		}

		public string GetTDP()
		{
			return "0";
		}

		public int CalCPUTemp(string CPUTDP, int set_remote)
		{
			bool flag = CPUTDP == "84W" || CPUTDP == "88W";
			double value;
			if (flag)
			{
				bool flag2 = set_remote > 60;
				if (flag2)
				{
					value = (double)(set_remote - 12) * 0.33 + 44.0;
				}
				else
				{
					value = (double)(set_remote - 1);
				}
			}
			else
			{
				bool flag3 = CPUTDP == "65W";
				if (flag3)
				{
					bool flag4 = set_remote > 50;
					if (flag4)
					{
						value = (double)(set_remote - 35) * 0.41 + 43.7;
					}
					else
					{
						value = (double)(set_remote - 1);
					}
				}
				else
				{
					bool flag5 = CPUTDP == "91W";
					if (flag5)
					{
						bool flag6 = set_remote > 50;
						if (flag6)
						{
							value = (double)(set_remote - 9) * 0.22 + 43.7;
						}
						else
						{
							value = (double)(set_remote - 5);
						}
					}
					else
					{
						bool flag7 = CPUTDP == "35W";
						if (flag7)
						{
							bool flag8 = set_remote > 32;
							if (flag8)
							{
								value = (double)set_remote * 0.5 + 16.0;
							}
							else
							{
								value = (double)set_remote;
							}
						}
						else
						{
							bool flag9 = CPUTDP == "47W";
							if (flag9)
							{
								bool flag10 = set_remote > 26;
								if (flag10)
								{
									value = (double)set_remote * 0.5 + 13.0;
								}
								else
								{
									value = (double)set_remote;
								}
							}
							else
							{
								value = (double)set_remote;
							}
						}
					}
				}
			}
			return (int)Math.Round(value, 0);
		}

		public void UpdateWMI12()
		{
			object value = Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\wmi", "WMI_12", "");
			byte[] array = (byte[])value;
			this.cpu_temp = this.CalCPUTemp(this.GetTDP(), (int)array[18]);
			this.gpu1_temp = (int)array[21];
			this.gpu2_temp = (int)array[24];
		}

		public int CpuTemp()
		{
			return this.cpu_temp;
		}

		public int GpuTemp(int num)
		{
			bool flag = num == 0;
			int result;
			if (flag)
			{
				result = this.gpu1_temp;
			}
			else
			{
				bool flag2 = num == 1;
				if (flag2)
				{
					result = this.gpu2_temp;
				}
				else
				{
					result = 0;
				}
			}
			return result;
		}

		public int FlexiKey_KB_Enable
		{
			get
			{
				return Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ComboKey\\", "KBEnable", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ComboKey", "KBEnable", value.ToString());
			}
		}

		public int FlexiKey_Mouse_Enable
		{
			get
			{
				return Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ComboKey\\", "MouseEnable", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ComboKey", "MouseEnable", value.ToString());
			}
		}

		public int FlexiAccessStatus
		{
			get
			{
				return Convert.ToInt32(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\FA", "FlexiAccessStatus", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\FA", "FlexiAccessStatus", value, RegistryValueKind.DWord);
			}
		}

		public string FA_ClientName
		{
			get
			{
				return Convert.ToString(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\FA", "ClientName", ""));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\FA", "ClientName", value, RegistryValueKind.String);
			}
		}

		public string FA_Port
		{
			get
			{
				return Convert.ToString(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\FA", "Port", ""));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\FA", "Port", value, RegistryValueKind.String);
			}
		}

		public string FA_IP
		{
			get
			{
				return Convert.ToString(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\FA", "IP", ""));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\FA", "IP", value, RegistryValueKind.String);
			}
		}

		public string last_RefreshDate
		{
			get
			{
				return Convert.ToString(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\BatteryUtility", "RefreshDate", "null"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\BatteryUtility", "RefreshDate", value, RegistryValueKind.String);
			}
		}

		public int OngoingStatus
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\BatteryUtility", "OngoingStatus", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\BatteryUtility", "OngoingStatus", value, RegistryValueKind.DWord);
			}
		}

		public int Refresh_ElapsedTime_ss
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\BatteryUtility", "Refresh_ElapsedTime_ss", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\BatteryUtility", "Refresh_ElapsedTime_ss", value, RegistryValueKind.DWord);
			}
		}

		public int Refresh_ElapsedTime_mm
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\BatteryUtility", "Refresh_ElapsedTime_mm", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\BatteryUtility", "Refresh_ElapsedTime_mm", value, RegistryValueKind.DWord);
			}
		}

		public int Refresh_ElapsedTime_hh
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\BatteryUtility", "Refresh_ElapsedTime_hh", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\BatteryUtility", "Refresh_ElapsedTime_hh", value, RegistryValueKind.DWord);
			}
		}

		public int BAT_log_count
		{
			get
			{
				return (int)Convert.ToInt16(Registry.GetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\BatteryUtility", "BAT_log_count", "0"));
			}
			set
			{
				Registry.SetValue("HKEY_CURRENT_USER\\Software\\ControlCenter2.0\\BatteryUtility", "BAT_log_count", value, RegistryValueKind.DWord);
			}
		}

		public string UI_language = "en-US";

		private const int CPU_REMOTE = 18;

		private const int GPU1_REMOTE = 21;

		private const int GPU2_REMOTE = 24;

		private int cpu_temp;

		private int gpu1_temp;

		private int gpu2_temp;

		public double barkup_VB_CPUFan_H;

		public double barkup_VB_CPUFan_W;

		public double barkup_VB_CPU_Temp_W;

		public double barkup_VB_CPU_Temp_H;

		public string CurrentMode;

		public string CurrentMode2;

		public int AirplaneMode;

		public int TouchPad;

		public int CCD;

		public int WhiteLedKB_Brightness;

		public int HeadPhone;

		public int KBSleepTimer;

		public int KBSleepTimerOn;

		public bool SupportNewMode = false;
	}
}
