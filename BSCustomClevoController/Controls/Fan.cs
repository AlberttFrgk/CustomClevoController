using System;
using BSCustomClevoController.Utility;


using static BSCustomClevoController.Utility.Structs;

namespace BSCustomClevoController.Controls
{
    internal class Fan
    {
        private static bool? _hasThirdFan;

        /// <summary>
        /// Indicates if the laptop motherboard and EC expose a 3rd cooling fan (GPU 2 Fan).
        /// </summary>
        public static bool HasThirdFan
        {
            get
            {
                if (!_hasThirdFan.HasValue)
                {
                    _hasThirdFan = DetectThirdFan();
                }
                return _hasThirdFan.Value;
            }
        }

        private static bool DetectThirdFan()
        {
            try
            {
                byte[] numArray = Global.insyde.GetWMIPackage(12);
                int raw_gpu2 = numArray[7] + (numArray[6] << 8);
                if (raw_gpu2 > 0 || numArray[22] > 0 || numArray[24] > 0)
                {
                    return true;
                }

                byte[] wmi13 = Global.insyde.GetWMIPackage(13);
                int fanCount = wmi13[12];
                if (fanCount >= 3)
                {
                    return true;
                }

                int fan3Info = 0;
                int res = InsydeDriver.GetDCHU_Data_Integer(110, ref fan3Info);
                if (res == 0 && fan3Info > 0)
                {
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        public static FanStatus GetStatus()
        {
            FanStatus status = new FanStatus
            {
                Mode = Mode.Auto,
                Offset = 0,
                Fan_CPU = new FanInfo(40, 35, 60, 55, 80, 80),
                Fan_GPU1 = new FanInfo(40, 35, 60, 55, 80, 80),
                Fan_GPU2 = new FanInfo(40, 35, 60, 55, 80, 80),
                HasThirdFan = HasThirdFan
            };

            try
            {
                byte[] appData = Global.insyde.GetAPPData(4, 0, 256);
                if (appData != null && appData.Length > 7)
                {
                    status.Mode = LookupMode(appData[5]);
                    status.Offset = appData[7];
                    Read_FanInfo(appData, ref status.Fan_CPU, ref status.Fan_GPU1, ref status.Fan_GPU2);
                }

                Read_FanSpeed(ref status.Fan_CPU.rpm, ref status.Fan_CPU.percent, ref status.Fan_CPU.remote,
                              ref status.Fan_GPU1.rpm, ref status.Fan_GPU1.percent, ref status.Fan_GPU1.remote,
                              ref status.Fan_GPU2.rpm, ref status.Fan_GPU2.percent, ref status.Fan_GPU2.remote);

                if (status.Mode == Mode.Auto || status.Mode == Mode.Max)
                {
                    SetProgramModeOnly(status.Mode);
                }
                else
                {
                    object regVal = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "FanMode", null);
                    if (regVal is int intMode)
                    {
                        status.Mode = LookupMode((byte)intMode);
                    }
                    else
                    {
                        SetMode(Mode.Auto);
                        status.Mode = Mode.Auto;
                    }
                }

                object regOffset = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "FanOffset", null);
                if (regOffset is int intOffset)
                {
                    status.Offset = (byte)Math.Max(0, Math.Min(100, intOffset));
                }
            }
            catch
            {
                status.Mode = Mode.Auto;
            }

            return status;
        }

        public static bool Read_FanSpeed(ref int rpm_CPU, ref int percent_CPU, ref int remote_CPU,
                                         ref int rpm_GPU, ref int percent_GPU, ref int remote_GPU)
        {
            int dummyRpm = 0, dummyPercent = 0, dummyRemote = 0;
            return Read_FanSpeed(ref rpm_CPU, ref percent_CPU, ref remote_CPU,
                                 ref rpm_GPU, ref percent_GPU, ref remote_GPU,
                                 ref dummyRpm, ref dummyPercent, ref dummyRemote);
        }

        private static int ConvertTachToRpm(int rawTach)
        {
            return (rawTach > 0 && rawTach < 65535)
                ? (int)Math.Round(60.0 / (5.56521739130435E-05 * rawTach) * 2.0, 0)
                : 0;
        }

        private static byte[] _fanSpeedCache;
        private static int _fanSpeedZeroStreak;

        public static bool Read_FanSpeed(ref int rpm_CPU, ref int percent_CPU, ref int remote_CPU,
                                         ref int rpm_GPU, ref int percent_GPU, ref int remote_GPU,
                                         ref int rpm_GPU2, ref int percent_GPU2, ref int remote_GPU2)
        {
            try
            {
                byte[] array = Global.insyde.GetWMIPackage(12);

                // A transient all-zero tach/duty/temp block means a failed EC read, not a stopped fan.
                bool blockLooksEmpty = array[2] == 0 && array[3] == 0 && array[4] == 0 && array[5] == 0
                                       && array[16] == 0 && array[19] == 0 && array[18] == 0 && array[21] == 0;
                if (blockLooksEmpty && _fanSpeedZeroStreak < 5 && _fanSpeedCache != null)
                {
                    _fanSpeedZeroStreak++;
                    array = _fanSpeedCache;
                }
                else
                {
                    _fanSpeedZeroStreak = blockLooksEmpty ? _fanSpeedZeroStreak : 0;
                    _fanSpeedCache = (byte[])array.Clone();
                }

                int rawCpu = (int)array[3] + ((int)array[2] << 8);
                rpm_CPU = ConvertTachToRpm(rawCpu);
                percent_CPU = (int)Math.Round((double)array[16] / 255.0 * 100.0, 0);
                if (percent_CPU == 0 && array[16] > 0)
                {
                    percent_CPU = array[16];
                }
                remote_CPU = Global.RW_REG != null ? Global.RW_REG.CalCPUTemp(Global.RW_REG.GetTDP(), (int)array[18]) : (int)array[18];

                int rawGpu = (int)array[5] + ((int)array[4] << 8);
                rpm_GPU = ConvertTachToRpm(rawGpu);
                percent_GPU = (int)Math.Round((double)array[19] / 255.0 * 100.0, 0);
                if (percent_GPU == 0 && array[19] > 0)
                {
                    percent_GPU = array[19];
                }
                remote_GPU = (int)array[21];

                int rawGpu2 = (int)array[7] + ((int)array[6] << 8);
                rpm_GPU2 = ConvertTachToRpm(rawGpu2);
                percent_GPU2 = (int)Math.Round((double)array[22] / 255.0 * 100.0, 0);
                if (percent_GPU2 == 0 && array[22] > 0)
                {
                    percent_GPU2 = array[22];
                }
                remote_GPU2 = array[24] > 0 ? (int)array[24] : remote_GPU;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void AntiDust()
        {
            try
            {
                _lastAntiDustTime = DateTime.UtcNow;
                Global.insyde.SetWMI(121, 41, 1u);
                Global.insyde.SetAPPData(4, 80, 1, new byte[] { 1 });
            }
            catch
            {
            }
        }

        public static bool IsAntiDustSupported()
        {
            try
            {
                if (Global.support_bit != null && Global.support_bit.AntiDust_Fan)
                {
                    return true;
                }

                if (Capabilities.IsSupported("SupportAntiDust_Fan"))
                {
                    return true;
                }

                int wmi96 = 0;
                int res = InsydeDriver.GetDCHU_Data_Integer(96, ref wmi96);
                if (res == 0 && ((wmi96 >> 7) & 1) == 1)
                {
                    if (Global.support_bit != null)
                    {
                        Global.support_bit.AntiDust_Fan = true;
                    }
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        public static bool IsMaxQSupported()
        {
            try
            {
                if (Global.support_bit != null && Global.support_bit.MaxQ)
                {
                    return true;
                }

                if (Global.RW_REG != null && Global.RW_REG.SupportMaxqFan)
                {
                    return true;
                }

                byte[] appData = Global.insyde.GetAPPData(4, 0, 256);
                if (appData != null && appData.Length > 5)
                {
                    if (appData[4] == 5 || appData[5] == 5)
                    {
                        if (Global.support_bit != null)
                        {
                            Global.support_bit.MaxQ = true;
                        }
                        return true;
                    }
                }

                byte[] wmi13 = Global.insyde.GetWMIPackage(13);
                if (wmi13 != null && wmi13.Length > 14)
                {
                    if (wmi13[14] == 5)
                    {
                        if (Global.support_bit != null)
                        {
                            Global.support_bit.MaxQ = true;
                        }
                        return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        public static void SetOffset(byte offset)
        {
            try
            {
                int data = (int)Math.Round(255.0 * ((double)offset / 100.0), 0);
                Global.insyde.SetWMI(121, 14, (uint)data);
                Global.insyde.SetAPPData(4, 7, 1, new byte[] { offset });
                try
                {
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "FanOffset", (int)offset, Microsoft.Win32.RegistryValueKind.DWord);
                }
                catch
                {
                }
            }
            catch
            {
            }
        }

        public static void SetMode(Mode mode)
        {
            try
            {
                _lastFanToggleTime = DateTime.UtcNow;
                SystemPreferences.UpdateHardwareFanModeCache(mode);
                Global.insyde.SetWMI(121, 1, (uint)mode);
                Global.insyde.SetAPPData(4, 5, 1, new byte[] { (byte)mode });
                try
                {
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "FanMode", (int)mode);
                }
                catch
                {
                }
            }
            catch
            {
            }
        }

        private static DateTime _lastFanToggleTime = DateTime.MinValue;
        private static DateTime _lastAntiDustTime = DateTime.MinValue;
        private static readonly object _fanToggleLock = new object();

        public static void SetProgramModeOnly(Mode mode)
        {
            try
            {
                Global.insyde.SetAPPData(4, 5, 1, new byte[] { (byte)mode });
            }
            catch
            {
            }
            try
            {
                Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "FanMode", (int)mode, Microsoft.Win32.RegistryValueKind.DWord);
            }
            catch
            {
            }
            SystemPreferences.UpdateHardwareFanModeCache(mode);
        }

        public static Mode GetSavedSoftwareFanMode()
        {
            try
            {
                object regVal = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "FanMode", null);
                if (regVal is int intMode)
                {
                    return LookupMode((byte)intMode);
                }
            }
            catch
            {
            }
            return Mode.Auto;
        }

        public static DateTime LastFanToggleTime => _lastFanToggleTime;

        public static Mode GetHardwareFanMode()
        {
            try
            {
                byte[] input = new byte[256];
                byte[] ram1 = new byte[256];
                input[0] = 3;
                if (Utility.InsydeDriver.SetDCHU_DataEx(4, input, 256, ref ram1[0]) > 0)
                {
                    byte oem4 = ram1[0xCC];
                    if (oem4 == 0xF3)
                    {
                        return Mode.Max;
                    }
                    if (oem4 == 0xF2 || oem4 == 0x6B)
                    {
                        return Mode.Auto;
                    }
                }
            }
            catch
            {
            }
            try
            {
                byte[] appData = Global.insyde.GetAPPData(4, 0, 256);
                if (appData != null && appData.Length > 5)
                {
                    return LookupMode(appData[5]);
                }
            }
            catch
            {
            }
            return Mode.Unknown;
        }

        public static void SyncFanFromHardware(Mode targetMode, bool showOsd = true)
        {
            if (targetMode != Mode.Auto && targetMode != Mode.Max)
            {
                return;
            }

            lock (_fanToggleLock)
            {
                if (DateTime.UtcNow < _lastFanToggleTime.AddMilliseconds(350))
                {
                    return;
                }
                _lastFanToggleTime = DateTime.UtcNow;

                try
                {
                    SetProgramModeOnly(targetMode);

                    if (showOsd)
                    {
                        OsdOverlay.ShowFan(targetMode);
                    }

                    System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        var mainWin = System.Windows.Application.Current.MainWindow as MainWindow;
                        mainWin?.SyncFanProfileUI(targetMode);
                    }));
                }
                catch
                {
                }
            }
        }

        public static void CheckAndSyncFanFromHardware(bool forceOsd = false)
        {
            if (DateTime.UtcNow < _lastFanToggleTime.AddMilliseconds(350))
            {
                return;
            }

            Mode hwMode = GetHardwareFanMode();
            if (hwMode == Mode.Auto || hwMode == Mode.Max)
            {
                object regVal = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "FanMode", null);
                Mode currentSwMode = (regVal is int intMode) ? LookupMode((byte)intMode) : Mode.Auto;

                if (forceOsd || hwMode != currentSwMode)
                {
                    SyncFanFromHardware(hwMode, showOsd: true);
                }
            }
        }

        public static void ToggleAutoMaxFan()
        {
            lock (_fanToggleLock)
            {
                if (DateTime.UtcNow < _lastFanToggleTime.AddMilliseconds(350))
                {
                    return;
                }
                _lastFanToggleTime = DateTime.UtcNow;

                try
                {
                    Mode hwMode = GetHardwareFanMode();
                    object regVal = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "FanMode", null);
                    Mode currentSwMode = (regVal is int intMode) ? LookupMode((byte)intMode) : Mode.Auto;

                    Mode targetMode;
                    if (hwMode == Mode.Auto || hwMode == Mode.Max)
                    {
                        if (hwMode != currentSwMode)
                        {
                            targetMode = hwMode;
                        }
                        else
                        {
                            targetMode = (currentSwMode == Mode.Max) ? Mode.Auto : Mode.Max;
                            SetMode(targetMode);
                        }
                    }
                    else
                    {
                        targetMode = (currentSwMode == Mode.Max) ? Mode.Auto : Mode.Max;
                        SetMode(targetMode);
                    }

                    SetProgramModeOnly(targetMode);
                    SystemPreferences.UpdateHardwareFanModeCache(targetMode);
                    OsdOverlay.ShowFan(targetMode);

                    System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        var mainWin = System.Windows.Application.Current.MainWindow as MainWindow;
                        mainWin?.SyncFanProfileUI(targetMode);
                    }));
                }
                catch
                {
                }
            }
        }

        public static void ApplyAutoFanFromHardware()
        {
            SyncFanFromHardware(Mode.Auto, showOsd: true);
        }

        public static void ApplyMaxFanFromHardware()
        {
            SyncFanFromHardware(Mode.Max, showOsd: true);
        }

        public static void ToggleMaxFan()
        {
            ToggleAutoMaxFan();
        }

        public enum Mode
        {
            Auto = 0,
            Max = 1,
            MaxQ = 5,
            Custom = 6,
            Unknown = 9
        }

        static Mode LookupMode(byte mode)
        {
            switch (mode)
            {
                case 0: return Mode.Auto;
                case 1: return Mode.Max;
                case 5: return Mode.MaxQ;
                case 6: return Mode.Custom;
                default: return Mode.Auto;
            }
        }

        public static void SetCustomFanTable(FanInfo CPU, FanInfo GPU)
        {
            try
            {
                byte[] buffer = new byte[256];
                buffer[2] = CPU.T2;
                buffer[3] = (byte)Math.Round(CPU.D2 / 100.0 * 255.0, 0);
                buffer[4] = CPU.T3;
                buffer[5] = (byte)Math.Round(CPU.D3 / 100.0 * 255.0, 0);
                buffer[6] = GPU.T2;
                buffer[7] = (byte)Math.Round(GPU.D2 / 100.0 * 255.0, 0);
                buffer[8] = GPU.T3;
                buffer[9] = (byte)Math.Round(GPU.D3 / 100.0 * 255.0, 0);
                buffer[10] = GPU.T2;
                buffer[11] = (byte)Math.Round(GPU.D2 / 100.0 * 255.0, 0);
                buffer[12] = GPU.T3;
                buffer[13] = (byte)Math.Round(GPU.D3 / 100.0 * 255.0, 0);

                int CPU_R12 = (int)Math.Round((CPU.D2 - CPU.D1) / (double)Math.Max(1, CPU.T2 - CPU.T1) * 2.55 * 16.0, 0);
                int CPU_R23 = (int)Math.Round((CPU.D3 - CPU.D2) / (double)Math.Max(1, CPU.T3 - CPU.T2) * 2.55 * 16.0, 0);
                int CPU_R34 = (int)Math.Round((CPU.D4 - CPU.D3) / (double)Math.Max(1, CPU.T4 - CPU.T3) * 2.55 * 16.0, 0);

                int GPU_R12 = (int)Math.Round((GPU.D2 - GPU.D1) / (double)Math.Max(1, GPU.T2 - GPU.T1) * 2.55 * 16.0, 0);
                int GPU_R23 = (int)Math.Round((GPU.D3 - GPU.D2) / (double)Math.Max(1, GPU.T3 - GPU.T2) * 2.55 * 16.0, 0);
                int GPU_R34 = (int)Math.Round((GPU.D4 - GPU.D3) / (double)Math.Max(1, GPU.T4 - GPU.T3) * 2.55 * 16.0, 0);

                buffer[14] = (byte)(CPU_R12 >> 8);
                buffer[15] = (byte)(CPU_R12 & 255);
                buffer[16] = (byte)(CPU_R23 >> 8);
                buffer[17] = (byte)(CPU_R23 & 255);
                buffer[18] = (byte)(CPU_R34 >> 8);
                buffer[19] = (byte)(CPU_R34 & 255);

                buffer[20] = (byte)(GPU_R12 >> 8);
                buffer[21] = (byte)(GPU_R12 & 255);
                buffer[22] = (byte)(GPU_R23 >> 8);
                buffer[23] = (byte)(GPU_R23 & 255);
                buffer[24] = (byte)(GPU_R34 >> 8);
                buffer[25] = (byte)(GPU_R34 & 255);

                buffer[26] = (byte)(GPU_R12 >> 8);
                buffer[27] = (byte)(GPU_R12 & 255);
                buffer[28] = (byte)(GPU_R23 >> 8);
                buffer[29] = (byte)(GPU_R23 & 255);
                buffer[30] = (byte)(GPU_R34 >> 8);
                buffer[31] = (byte)(GPU_R34 & 255);

                Global.insyde.SetWMIPackage(14, buffer);
                SetMode(Mode.Custom);
                SetFanInfo(CPU, GPU);
            }
            catch
            {
            }
        }

        static void SetFanInfo(FanInfo CPU, FanInfo GPU)
        {
            try
            {
                byte[] currentAppData = Global.insyde.GetAPPData(4, 0, 256);
                byte offsetVal = currentAppData[7];
                byte modeVal = (byte)Mode.Custom;

                byte[] buffer = new byte[256];
                buffer[0] = 3;
                buffer[1] = 0;
                buffer[2] = 0;
                buffer[3] = 0;
                buffer[4] = 8;
                buffer[5] = modeVal;
                buffer[6] = 1;
                buffer[7] = offsetVal;

                buffer[16] = CPU.D1;
                buffer[17] = CPU.D2;
                buffer[18] = CPU.D3;
                buffer[19] = CPU.D4;
                buffer[20] = CPU.D2;
                buffer[21] = CPU.D3;
                buffer[22] = CPU.T1;
                buffer[23] = CPU.T2;
                buffer[24] = CPU.T3;
                buffer[25] = CPU.T4;
                buffer[26] = CPU.T2;
                buffer[27] = CPU.T3;

                int CPU_R12 = (int)Math.Round((CPU.D2 - CPU.D1) / (double)Math.Max(1, CPU.T2 - CPU.T1) * 2.55 * 16.0, 0);
                int CPU_R23 = (int)Math.Round((CPU.D3 - CPU.D2) / (double)Math.Max(1, CPU.T3 - CPU.T2) * 2.55 * 16.0, 0);
                int CPU_R34 = (int)Math.Round((CPU.D4 - CPU.D3) / (double)Math.Max(1, CPU.T4 - CPU.T3) * 2.55 * 16.0, 0);

                buffer[28] = (byte)(CPU_R12 & 255);
                buffer[29] = (byte)(CPU_R12 >> 8);
                buffer[30] = (byte)(CPU_R23 & 255);
                buffer[31] = (byte)(CPU_R23 >> 8);
                buffer[32] = (byte)(CPU_R34 & 255);
                buffer[33] = (byte)(CPU_R34 >> 8);

                buffer[34] = GPU.D1;
                buffer[35] = GPU.D2;
                buffer[36] = GPU.D3;
                buffer[37] = GPU.D4;
                buffer[38] = GPU.D2;
                buffer[39] = GPU.D3;
                buffer[40] = GPU.T1;
                buffer[41] = GPU.T2;
                buffer[42] = GPU.T3;
                buffer[43] = GPU.T4;
                buffer[44] = GPU.T2;
                buffer[45] = GPU.T3;

                int GPU_R12 = (int)Math.Round((GPU.D2 - GPU.D1) / (double)Math.Max(1, GPU.T2 - GPU.T1) * 2.55 * 16.0, 0);
                int GPU_R23 = (int)Math.Round((GPU.D3 - GPU.D2) / (double)Math.Max(1, GPU.T3 - GPU.T2) * 2.55 * 16.0, 0);
                int GPU_R34 = (int)Math.Round((GPU.D4 - GPU.D3) / (double)Math.Max(1, GPU.T4 - GPU.T3) * 2.55 * 16.0, 0);

                buffer[46] = (byte)(GPU_R12 & 255);
                buffer[47] = (byte)(GPU_R12 >> 8);
                buffer[48] = (byte)(GPU_R23 & 255);
                buffer[49] = (byte)(GPU_R23 >> 8);
                buffer[50] = (byte)(GPU_R34 & 255);
                buffer[51] = (byte)(GPU_R34 >> 8);

                buffer[52] = GPU.D1;
                buffer[53] = GPU.D2;
                buffer[54] = GPU.D3;
                buffer[55] = GPU.D4;
                buffer[56] = GPU.D2;
                buffer[57] = GPU.D3;
                buffer[58] = GPU.T1;
                buffer[59] = GPU.T2;
                buffer[60] = GPU.T3;
                buffer[61] = GPU.T4;
                buffer[62] = GPU.T2;
                buffer[63] = GPU.T3;

                buffer[64] = (byte)(GPU_R12 & 255);
                buffer[65] = (byte)(GPU_R12 >> 8);
                buffer[66] = (byte)(GPU_R23 & 255);
                buffer[67] = (byte)(GPU_R23 >> 8);
                buffer[68] = (byte)(GPU_R34 & 255);
                buffer[69] = (byte)(GPU_R34 >> 8);

                Global.insyde.SetAPPData(4, 0, 256, buffer);
            }
            catch
            {
            }
        }

        static void Read_FanInfo(byte[] array, ref FanInfo Fan_CPU, ref FanInfo Fan_GPU1, ref FanInfo Fan_GPU2)
        {
            try
            {
                if (array != null && array.Length >= 70)
                {
                    if (array[22] > 0 && array[23] > 0)
                    {
                        Fan_CPU.D1 = (array[19] > 0) ? array[16] : (array[16] > 0 ? array[16] : (byte)35);
                        Fan_CPU.D2 = array[17] > 0 ? array[17] : (byte)50;
                        Fan_CPU.D3 = array[18] > 0 ? array[18] : (byte)85;
                        Fan_CPU.D4 = array[19] > 0 ? array[19] : (byte)100;
                        Fan_CPU.D2_Default = array[20];
                        Fan_CPU.D3_Default = array[21];
                        Fan_CPU.T1 = array[22] > 0 ? array[22] : (byte)40;
                        Fan_CPU.T2 = array[23] > 0 ? array[23] : (byte)60;
                        Fan_CPU.T3 = array[24] > 0 ? array[24] : (byte)80;
                        Fan_CPU.T4 = array[25] > 0 ? array[25] : (byte)100;
                        Fan_CPU.T2_Default = array[26];
                        Fan_CPU.T3_Default = array[27];
                        Fan_CPU.R12 = (array[29] << 8) + array[28];
                        Fan_CPU.R23 = (array[31] << 8) + array[30];
                        Fan_CPU.R34 = (array[33] << 8) + array[32];
                    }

                    if (array[40] > 0 && array[41] > 0)
                    {
                        Fan_GPU1.D1 = (array[37] > 0) ? array[34] : (array[34] > 0 ? array[34] : (byte)35);
                        Fan_GPU1.D2 = array[35] > 0 ? array[35] : (byte)50;
                        Fan_GPU1.D3 = array[36] > 0 ? array[36] : (byte)85;
                        Fan_GPU1.D4 = array[37] > 0 ? array[37] : (byte)100;
                        Fan_GPU1.D2_Default = array[38];
                        Fan_GPU1.D3_Default = array[39];
                        Fan_GPU1.T1 = array[40] > 0 ? array[40] : (byte)40;
                        Fan_GPU1.T2 = array[41] > 0 ? array[41] : (byte)60;
                        Fan_GPU1.T3 = array[42] > 0 ? array[42] : (byte)80;
                        Fan_GPU1.T4 = array[43] > 0 ? array[43] : (byte)100;
                        Fan_GPU1.T2_Default = array[44];
                        Fan_GPU1.T3_Default = array[45];
                        Fan_GPU1.R12 = (array[47] << 8) + array[46];
                        Fan_GPU1.R23 = (array[49] << 8) + array[48];
                        Fan_GPU1.R34 = (array[51] << 8) + array[50];
                    }

                    if (array[58] > 0 && array[59] > 0)
                    {
                        Fan_GPU2.D1 = (array[55] > 0) ? array[52] : (array[52] > 0 ? array[52] : (byte)35);
                        Fan_GPU2.D2 = array[53] > 0 ? array[53] : (byte)50;
                        Fan_GPU2.D3 = array[54] > 0 ? array[54] : (byte)85;
                        Fan_GPU2.D4 = array[55] > 0 ? array[55] : (byte)100;
                        Fan_GPU2.D2_Default = array[56];
                        Fan_GPU2.D3_Default = array[57];
                        Fan_GPU2.T1 = array[58] > 0 ? array[58] : (byte)40;
                        Fan_GPU2.T2 = array[59] > 0 ? array[59] : (byte)60;
                        Fan_GPU2.T3 = array[60] > 0 ? array[60] : (byte)80;
                        Fan_GPU2.T4 = array[61] > 0 ? array[61] : (byte)100;
                        Fan_GPU2.T2_Default = array[62];
                        Fan_GPU2.T3_Default = array[63];
                        Fan_GPU2.R12 = (array[65] << 8) + array[64];
                        Fan_GPU2.R23 = (array[67] << 8) + array[66];
                        Fan_GPU2.R34 = (array[69] << 8) + array[68];
                    }
                }
            }
            catch
            {
            }
        }
    }
}