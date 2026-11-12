using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using BSCustomClevoController.Controls;
using static BSCustomClevoController.Utility.Structs;

namespace BSCustomClevoController.Utility
{
    public static class SystemPreferences
    {
        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessageData(IntPtr hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        [DllImport("ole32.dll")]
        private static extern int CoInitialize(IntPtr pv);

        [DllImport("ole32.dll")]
        private static extern void CoUninitialize();

        [DllImport("ole32.dll")]
        private static extern uint CoCreateInstance(ref Guid clsid, IntPtr pv, uint ctx, ref Guid iid, out IntPtr ppv);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetSystemRadioStateDelegate(IntPtr cg, out int radioState, out int settingSource, out int p3);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int SetSystemRadioStateDelegate(IntPtr ptr, int state);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int ReleaseDelegate(IntPtr ptr);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private static readonly Guid CLSID_RadioManager = new Guid("581333F6-28DB-41BE-BC7A-FF201F12F3F6");
        private static readonly Guid IID_RadioManager = new Guid("DB3AFBFB-08E6-46C6-AA70-BF9A34C30AB7");
        private static readonly Guid GUID_DEVCLASS_CAMERA = new Guid("ca3e7ab9-b4c3-4ae6-8251-579ef933890f");
        private static readonly Guid GUID_DEVCLASS_IMAGE = new Guid("6bdd1fc6-810f-11d0-bec7-08002be2092f");
        private static readonly Guid GUID_DEVCLASS_DISPLAY = new Guid("4d36e968-e325-11ce-bfc1-08002be10318");

        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_ALLCLASSES = 0x00000004;
        private const uint DIF_PROPERTYCHANGE = 0x00000012;
        private const uint DICS_ENABLE = 0x00000001;
        private const uint DICS_DISABLE = 0x00000002;
        private const uint DICS_FLAG_GLOBAL = 0x00000001;
        private const uint DICS_FLAG_CONFIGSPECIFIC = 0x00000002;
        private const uint DN_HAS_PROBLEM = 0x00000400;
        private const uint CM_PROB_DISABLED = 0x00000016;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_CLASSINSTALL_HEADER
        {
            public int cbSize;
            public int InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_PROPCHANGE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader;
            public int StateChange;
            public int Scope;
            public int HwProfile;
        }

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid,
            [MarshalAs(UnmanagedType.LPStr)] string Enumerator,
            IntPtr hwndParent,
            uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet,
            uint MemberIndex,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiSetClassInstallParams(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref SP_PROPCHANGE_PARAMS ClassInstallParams,
            int ClassInstallParamsSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiCallClassInstaller(
            uint InstallFunction,
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInstanceId(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            System.Text.StringBuilder DeviceInstanceId,
            int DeviceInstanceIdSize,
            out int RequiredSize);

        [DllImport("cfgmgr32.dll", SetLastError = true)]
        private static extern uint CM_Get_DevNode_Status(
            out uint pulStatus,
            out uint pulProblemNumber,
            uint dnDevInst,
            uint ulFlags);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int VK_ESCAPE = 0x1B;
        private const int VK_CAPITAL = 0x14;
        private const int VK_NUMLOCK = 0x90;
        private const int VK_SCROLL = 0x91;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        private const int VK_VOLUME_MUTE = 0xAD;
        private const int VK_VOLUME_DOWN = 0xAE;
        private const int VK_VOLUME_UP = 0xAF;
        private const int VK_MEDIA_NEXT_TRACK = 0xB0;
        private const int VK_MEDIA_PREV_TRACK = 0xB1;
        private const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const int VK_AIRPLANE = 0x84;

        private const uint WM_SYSCOMMAND = 0x0112;
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
        private static readonly IntPtr SC_MONITORPOWER = new IntPtr(0xF170);
        private static readonly IntPtr MONITOR_OFF = new IntPtr(2);

        private static readonly LowLevelKeyboardProc _hookCallback = HookProcedure;
        private static readonly HashSet<uint> _handledFnKeys = new HashSet<uint>();
        private static IntPtr _hookId = IntPtr.Zero;
        private static bool _isWinKeyLocked = false;
        private static bool _isSwapWinFn = false;
        private static bool _isFnLock = false;
        private static DateTime _suppressEcKeyboardEventsUntilUtc = DateTime.MinValue;
        private static byte[] _lastEcKeyboardState = new byte[16];
        [StructLayout(LayoutKind.Sequential)]
        private struct ACPI_HSA_WAIT_EVENT
        {
            public IntPtr HsaEvent;
        }

        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateFileW(
            string filename,
            uint access,
            uint sharing,
            IntPtr securityAttributes,
            uint mode,
            uint options,
            IntPtr template);

        [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
        private static extern bool DeviceIoControlWaitEvent(
            IntPtr hDevice,
            uint dwIoControlCode,
            ref ACPI_HSA_WAIT_EVENT inBuffer,
            int inBufferSize,
            byte[] outBuffer,
            uint outBufferSize,
            ref uint bytesReturned,
            IntPtr overlapped);

        [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
        private static extern bool DeviceIoControlGetNotify(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr inBuffer,
            int inBufferSize,
            ref long outBuffer,
            uint outBufferSize,
            ref uint bytesReturned,
            IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ResetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private static System.Threading.Thread _hsaListenerThread;
        private static volatile bool _hsaRunning = false;
        private static EventLog _outLogWatcher;
        private static ManagementEventWatcher _clevoEventWatcher;
        private static System.Threading.Timer _hardwareStatePollTimer;
        private static bool? _lastAirplaneState;
        private static bool? _lastCameraState;
        private static DateTime _suppressAirplaneOsdUntil = DateTime.MinValue;
        private static DateTime _suppressCameraOsdUntil = DateTime.MinValue;
        private static bool? _lastMicState;
        private static DateTime _suppressMicOsdUntil = DateTime.MinValue;
        private static DateTime _lastMicToggleUtc = DateTime.MinValue;
        private static DateTime _lastWinKeyChangeTime = DateTime.MinValue;
        public static DateTime LastWinKeyChangeTime => _lastWinKeyChangeTime;
        private static DateTime _lastWinKeyOsdTime = DateTime.MinValue;
        private static bool _hookWinDown = false;
        private static bool _hookAltDown = false;

        public static bool IsSwapWinFnSupported { get; private set; } = true;

        public static void SetAcpiEventListening(bool enable)
        {
            try
            {
                byte[] zero4 = new byte[4];
                InsydeDriver.WriteAppSettings(0, 8, 4, ref zero4[0]);

                byte[] status = new byte[1] { (byte)(enable ? 1 : 0) };
                InsydeDriver.WriteAppSettings(1, 12, 1, ref status[0]);

                if (enable)
                {
                    int dchu70 = 0;
                    InsydeDriver.GetDCHU_Data_Integer(70, ref dchu70);

                    byte[] buf121_17 = new byte[4] { 1, 0, 0, 17 };
                    InsydeDriver.SetDCHU_Data(121, buf121_17, 4);

                    byte[] buf121_26 = new byte[4] { 1, 0, 0, 26 };
                    InsydeDriver.SetDCHU_Data(121, buf121_26, 4);

                    byte[] buf121_34 = new byte[4] { 1, 0, 0, 34 };
                    InsydeDriver.SetDCHU_Data(121, buf121_34, 4);

                    ClearEcHotkeyByte();
                }
            }
            catch
            {
            }
        }

        private static readonly object _ecPollLock = new object();
        private static byte _lastOem4 = 0;

        private static void ClearEcHotkeyByte()
        {
            try
            {
                byte[] addr = new byte[4] { 0xCC, 0, 0, 0 };
                byte[] val = new byte[4] { 0, 0, 0, 0 };
                InsydeDriver.SetDCHU_Data(107, addr, 4);
                InsydeDriver.SetDCHU_Data(117, val, 4);
            }
            catch
            {
            }
        }

        private static void PollEcHotkeysAndFanState()
        {
            lock (_ecPollLock)
            {
                try
                {
                    byte[] input = new byte[256];
                    byte[] ram1 = new byte[256];
                    input[0] = 3;
                    if (InsydeDriver.SetDCHU_DataEx(4, input, 256, ref ram1[0]) <= 0)
                    {
                        return;
                    }

                    // Keep ACPI hotkey dispatch active if EC/BIOS reset HKDR/AIRP
                    if ((ram1[0xD9] & 0x10) == 0 && ram1[0xD9] != 0)
                    {
                        int dchu70 = 0;
                        InsydeDriver.GetDCHU_Data_Integer(70, ref dchu70);
                        byte[] buf121_34 = new byte[4] { 1, 0, 0, 34 };
                        InsydeDriver.SetDCHU_Data(121, buf121_34, 4);
                    }

                    byte oem4 = ram1[0xCC];
                    if (oem4 == 0)
                    {
                        _lastOem4 = 0;
                        return;
                    }

                    ClearEcHotkeyByte();
                    if (oem4 == _lastOem4)
                    {
                        return;
                    }
                    _lastOem4 = oem4;

                    switch (oem4)
                    {
                        case 0x80:
                            HandleAcpiEvent(131);
                            break;
                        case 0x81:
                        case 0x20:
                            HandleAcpiEvent(129);
                            break;
                        case 0x82:
                        case 0x21:
                            HandleAcpiEvent(130);
                            break;
                        case 0x8A:
                            HandleAcpiEvent(138);
                            break;
                        case 0x9E:
                            HandleAcpiEvent(158);
                            break;
                        case 0x9F:
                        case 0x3F:
                            HandleAcpiEvent(159);
                            break;
                        case 0xF2:
                        case 0x6B:
                            HandleAcpiEvent(112);
                            break;
                        case 0xF3:
                            HandleAcpiEvent(143);
                            break;
                        case 0xFA:
                            HandleAcpiEvent(38);
                            break;
                        case 0xFB:
                            HandleAcpiEvent(37);
                            break;
                        case 0x59:
                            HandleAcpiEvent(89);
                            break;
                    }
                }
                catch
                {
                }
            }
        }

        public static void Initialize()
        {
            if (AcpiDriverInstaller.IsDriverWorking())
            {
                SetAcpiEventListening(true);
            }
            Touchpad.Initialize();
            StartHsaEventListener();
            StartWmiEventWatcher();
            StartEventLogWatcher();
            StartHardwareStateWatcher();
            UpdateHookState();

            // Release any modifier keys left stuck by boot/startup tools or drivers
            try
            {
                keybd_event((byte)VK_LWIN, 0, 0x0002 | 0x0001, 0);
                keybd_event(0x11, 0, 0x0002, 0);
                keybd_event(0x10, 0, 0x0002, 0);
                keybd_event(0x12, 0, 0x0002, 0);
                _hookWinDown = false;
                _hookAltDown = false;
            }
            catch
            {
            }
        }

        static SystemPreferences()
        {
            try
            {
                if (AcpiDriverInstaller.IsDriverWorking())
                {
                    SetAcpiEventListening(true);
                }
                Touchpad.Initialize();
                QueryKeyboardSettingsFromEC();

                object fnVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "FnKeyLock", null);
                if (fnVal is int intFn) _isFnLock = intFn != 0;

                object swapVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "SwapWinFn", null);
                if (swapVal is int intSwap) _isSwapWinFn = intSwap != 0;

                object winLockVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "WinKeyLock", null);
                if (winLockVal is int intWinLock) _isWinKeyLocked = intWinLock != 0;

                ApplyKeyboardSettingsToEC();
                StartHsaEventListener();
                StartWmiEventWatcher();
                StartEventLogWatcher();
                StartHardwareStateWatcher();
                UpdateHookState();
            }
            catch
            {
            }
        }

        public static bool IsWinKeyLocked => _isWinKeyLocked;
        public static bool IsSwapWinFn => _isSwapWinFn;
        public static bool IsFnLock => _isFnLock;

        public static void TurnOffScreen()
        {
            PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, MONITOR_OFF);
            try
            {
                byte[] buffer = new byte[4];
                buffer[0] = 1;
                InsydeDriver.SetDCHU_Data(275, buffer, 4);
            }
            catch
            {
            }
        }

        public static void StartWmiEventWatcher()
        {
            try
            {
                if (_clevoEventWatcher != null) return;
                ManagementScope scope = new ManagementScope(@"root\WMI");
                scope.Connect();
                WqlEventQuery query = new WqlEventQuery("SELECT * FROM CLEVO_EVENT");
                _clevoEventWatcher = new ManagementEventWatcher(scope, query);
                _clevoEventWatcher.EventArrived += (s, e) =>
                {
                    try
                    {
                        using (ManagementObject clevoObj = new ManagementObject("root\\WMI", "CLEVO_GET.InstanceName='ACPI\\PNP0C14\\0_0'", null))
                        using (ManagementBaseObject outParams = clevoObj.InvokeMethod("GetEvent", null, null))
                        {
                            if (outParams != null && outParams["Data"] != null)
                            {
                                string text = outParams["Data"].ToString();
                                if (int.TryParse(text, out int eventCode))
                                {
                                    HandleAcpiEvent(eventCode);
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                };
                _clevoEventWatcher.Start();
            }
            catch
            {
            }
        }

        public static void StopWmiEventWatcher()
        {
            if (_clevoEventWatcher != null)
            {
                try
                {
                    _clevoEventWatcher.Stop();
                    _clevoEventWatcher.Dispose();
                }
                catch
                {
                }
                _clevoEventWatcher = null;
            }
        }

        public static void StartHsaEventListener()
        {
            if (_hsaRunning) return;
            _hsaRunning = true;
            _hsaListenerThread = new System.Threading.Thread(HsaListenerWorker)
            {
                IsBackground = true,
                Name = "ClevoHsaEventListener"
            };
            _hsaListenerThread.Start();
        }

        public static void StopHsaEventListener()
        {
            _hsaRunning = false;
            if (_hsaListenerThread != null)
            {
                if (_hsaListenerThread.IsAlive)
                {
                    _hsaListenerThread.Join(1000);
                }
                _hsaListenerThread = null;
            }
        }

        private static void HsaListenerWorker()
        {
            const string AcpiBridgeDevicePath = @"\\?\ACPI#CLV0001#1#{86994c74-ad43-4812-b7e7-0c420b5c5fd7}";
            const uint GENERIC_READ = 0x80000000;
            const uint GENERIC_WRITE = 0x40000000;
            const uint FILE_SHARE_READ = 0x00000001;
            const uint FILE_SHARE_WRITE = 0x00000002;
            const uint OPEN_EXISTING = 3;
            const uint IOCTL_ACPIBIOS_WAIT_EVENT = 3286020u;
            const uint IOCTL_ACPIBIOS_GET_NOTIFY_VALUE = 3286024u;

            while (_hsaRunning)
            {
                IntPtr hDevice = CreateFileW(AcpiBridgeDevicePath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (hDevice == IntPtr.Zero || hDevice == new IntPtr(-1))
                {
                    PollEcHotkeysAndFanState();
                    System.Threading.Thread.Sleep(100);
                    continue;
                }

                IntPtr hsaEvent = CreateEvent(IntPtr.Zero, true, false, null);
                try
                {
                    SetAcpiEventListening(true);
                    ACPI_HSA_WAIT_EVENT waitStruct = new ACPI_HSA_WAIT_EVENT { HsaEvent = hsaEvent };
                    byte[] waitOutBuf = new byte[256];
                    uint returned = 0;
                    DeviceIoControlWaitEvent(hDevice, IOCTL_ACPIBIOS_WAIT_EVENT, ref waitStruct, Marshal.SizeOf(typeof(ACPI_HSA_WAIT_EVENT)), waitOutBuf, 256u, ref returned, IntPtr.Zero);
                    ResetEvent(hsaEvent);

                    while (_hsaRunning)
                    {
                        uint waitResult = WaitForSingleObject(hsaEvent, 80);
                        if (waitResult == 0)
                        {
                            ResetEvent(hsaEvent);
                            long notifyValue = 0;
                            if (DeviceIoControlGetNotify(hDevice, IOCTL_ACPIBIOS_GET_NOTIFY_VALUE, IntPtr.Zero, 0, ref notifyValue, 8, ref returned, IntPtr.Zero))
                            {
                                if (notifyValue != 0)
                                {
                                    HandleAcpiEvent((int)notifyValue);
                                    ClearEcHotkeyByte();
                                    DeviceIoControlWaitEvent(hDevice, IOCTL_ACPIBIOS_WAIT_EVENT, ref waitStruct, Marshal.SizeOf(typeof(ACPI_HSA_WAIT_EVENT)), waitOutBuf, 256u, ref returned, IntPtr.Zero);
                                }
                            }
                        }
                        else
                        {
                            PollEcHotkeysAndFanState();
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    if (hsaEvent != IntPtr.Zero) CloseHandle(hsaEvent);
                    CloseHandle(hDevice);
                }
            }
        }

        public static void StartEventLogWatcher()
        {
            try
            {
                if (_outLogWatcher != null) return;
                if (!EventLog.Exists("OutLog")) return;

                _outLogWatcher = new EventLog("OutLog") { Source = "PowerBIOSServer_Out" };
                _outLogWatcher.EntryWritten += (s, e) =>
                {
                    try
                    {
                        string msg = e.Entry.Message;
                        if (!string.IsNullOrEmpty(msg) && msg.StartsWith("Arg001^"))
                        {
                            string[] parts = msg.Split('^');
                            if (parts.Length >= 2 && int.TryParse(parts[1].Trim(), out int eventCode))
                            {
                                HandleAcpiEvent(eventCode);
                            }
                        }
                    }
                    catch
                    {
                    }
                };
                _outLogWatcher.EnableRaisingEvents = true;
            }
            catch
            {
            }
        }

        public static void StopEventLogWatcher()
        {
            if (_outLogWatcher != null)
            {
                try
                {
                    _outLogWatcher.EnableRaisingEvents = false;
                    _outLogWatcher.Dispose();
                }
                catch
                {
                }
                _outLogWatcher = null;
            }
        }

        public static void Shutdown()
        {
            StopHsaEventListener();
            SetAcpiEventListening(false);
            StopWmiEventWatcher();
            StopEventLogWatcher();
            StopHardwareStateWatcher();
            RemoveKeyboardHook();
        }

        private static bool? _lastAcState;
        private static Controls.Fan.Mode? _lastHardwareFanMode;

        internal static void UpdateHardwareFanModeCache(Controls.Fan.Mode mode)
        {
            _lastHardwareFanMode = mode;
        }

        private static void StartHardwareStateWatcher()
        {
            if (_hardwareStatePollTimer != null) return;
            _lastAirplaneState = GetAirplaneModeState();
            _lastCameraState = GetCameraState();
            _lastMicState = GetMicrophoneState();
            _lastAcState = System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;
            _lastHardwareFanMode = Controls.Fan.GetHardwareFanMode();
            _hardwareStatePollTimer = new System.Threading.Timer(PollHardwareStates, null, 500, 400);

            try
            {
                Microsoft.Win32.SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            }
            catch
            {
            }
        }

        private static void StopHardwareStateWatcher()
        {
            try
            {
                Microsoft.Win32.SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            }
            catch
            {
            }

            if (_hardwareStatePollTimer != null)
            {
                try
                {
                    _hardwareStatePollTimer.Dispose();
                }
                catch
                {
                }
                _hardwareStatePollTimer = null;
            }
        }

        private static void SystemEvents_PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
        {
            if (e.Mode == Microsoft.Win32.PowerModes.Resume)
            {
                SetAcpiEventListening(true);
            }
            if (e.Mode == Microsoft.Win32.PowerModes.StatusChange || e.Mode == Microsoft.Win32.PowerModes.Resume)
            {
                bool ac = System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;
                _lastAcState = ac;
                KeyboardEffects.HandlePowerStatusChanged(ac);
                System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    (System.Windows.Application.Current.MainWindow as MainWindow)?.SyncPowerLineUI(ac);
                }));
            }
        }

        private static void PollHardwareStates(object state)
        {
            try
            {
                bool currentAirplane = GetAirplaneModeState();
                if (!_lastAirplaneState.HasValue)
                {
                    _lastAirplaneState = currentAirplane;
                }
                else if (_lastAirplaneState.Value != currentAirplane)
                {
                    _lastAirplaneState = currentAirplane;
                    if (DateTime.UtcNow >= _suppressAirplaneOsdUntil)
                    {
                        OsdOverlay.ShowAirplane(currentAirplane);
                    }
                }

                bool currentMic = GetMicrophoneState();
                if (!_lastMicState.HasValue)
                {
                    _lastMicState = currentMic;
                }
                else if (_lastMicState.Value != currentMic)
                {
                    _lastMicState = currentMic;
                    if (DateTime.UtcNow >= _suppressMicOsdUntil)
                    {
                        OsdOverlay.ShowMicrophone(currentMic);
                    }
                }

                bool currentCamera = GetCameraState();
                if (!_lastCameraState.HasValue)
                {
                    _lastCameraState = currentCamera;
                }
                else if (_lastCameraState.Value != currentCamera)
                {
                    _lastCameraState = currentCamera;
                    if (DateTime.UtcNow >= _suppressCameraOsdUntil)
                    {
                        OsdOverlay.ShowCamera(currentCamera);
                    }
                }

                bool currentAc = System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;
                if (!_lastAcState.HasValue)
                {
                    _lastAcState = currentAc;
                }
                else if (_lastAcState.Value != currentAc)
                {
                    _lastAcState = currentAc;
                    KeyboardEffects.HandlePowerStatusChanged(currentAc);
                    System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        (System.Windows.Application.Current.MainWindow as MainWindow)?.SyncPowerLineUI(currentAc);
                    }));
                }
            }
            catch
            {
            }
        }

        private static int _lastAcpiEventCode = -1;
        private static DateTime _lastAcpiEventTime = DateTime.MinValue;
        private static DateTime _lastHardwareTouchpadKeyUtc = DateTime.MinValue;
        private static readonly object _acpiEventLock = new object();

        private static void HandleAcpiEvent(int rawEventCode)
        {
            int eventCode = (rawEventCode == 259 || rawEventCode == 303 || rawEventCode == 304 ||
                             rawEventCode == 24600 || rawEventCode == 24700 || rawEventCode == 24900)
                ? rawEventCode
                : (rawEventCode & 0xFF);

            lock (_acpiEventLock)
            {
                DateTime now = DateTime.UtcNow;
                if (eventCode == _lastAcpiEventCode && (now - _lastAcpiEventTime).TotalMilliseconds < 250)
                {
                    return;
                }
                _lastAcpiEventCode = eventCode;
                _lastAcpiEventTime = now;
            }

            switch (eventCode)
            {
                case 37:
                    if (DateTime.UtcNow >= _suppressEcKeyboardEventsUntilUtc)
                    {
                        ToggleFnLockFromEC();
                    }
                    break;
                case 38:
                    ToggleMicrophone();
                    break;
                case 58:
                    Fan.ToggleAutoMaxFan();
                    break;
                case 112:
                    Fan.SyncFanFromHardware(Fan.Mode.Auto, showOsd: true);
                    break;
                case 143:
                    Fan.SyncFanFromHardware(Fan.Mode.Max, showOsd: true);
                    break;
                case 64:
                    OsdOverlay.ShowFanAntiDust();
                    break;
                case 106:
                    Fan.SetMode(Fan.Mode.MaxQ);
                    OsdOverlay.ShowFan(Fan.Mode.MaxQ);
                    (System.Windows.Application.Current?.MainWindow as MainWindow)?.SyncFanProfileUI(Fan.Mode.MaxQ);
                    break;
                case 107:
                    Fan.SetMode(Fan.Mode.Custom);
                    OsdOverlay.ShowFan(Fan.Mode.Custom);
                    (System.Windows.Application.Current?.MainWindow as MainWindow)?.SyncFanProfileUI(Fan.Mode.Custom);
                    break;
                case 89:
                    PowerMode.CyclePowerMode();
                    break;
                case 84:
                    TurnOffScreen();
                    break;
                case 252:
                    Touchpad.HandleHardwareStateChange(false);
                    break;
                case 253:
                    Touchpad.HandleHardwareStateChange(true);
                    break;
                case 259:
                    System.Threading.Tasks.Task.Delay(60).ContinueWith(_ =>
                    {
                        if ((DateTime.UtcNow - _lastHardwareTouchpadKeyUtc).TotalMilliseconds >= 800 && DateTime.UtcNow >= Touchpad.SuppressHardwareEventsUntilUtc)
                        {
                            _lastHardwareTouchpadKeyUtc = DateTime.UtcNow;
                            Touchpad.ToggleTouchpad();
                        }
                    });
                    break;
                case 138:
                    CycleKeyboardBacklight();
                    break;
                case 32:
                case 129:
                    {
                        int? step = KeyboardBackLight.DecreaseBrightness();
                        if (step.HasValue)
                        {
                            OsdOverlay.ShowBacklight(step.Value);
                        }
                    }
                    break;
                case 33:
                case 130:
                    {
                        int? step = KeyboardBackLight.IncreaseBrightness();
                        if (step.HasValue)
                        {
                            OsdOverlay.ShowBacklight(step.Value);
                        }
                    }
                    break;
                case 63:
                case 158:
                case 159:
                    OsdOverlay.ShowBacklightToggle(KeyboardBackLight.ToggleBacklight());
                    break;
                case 131:
                    {
                        var preset = KeyboardBackLight.CycleColorPreset();
                        if (preset.HasValue)
                        {
                            OsdOverlay.ShowBacklightColor(preset.Value.Name);
                        }
                    }
                    break;
                case 132:
                    ToggleCamera();

                    break;
                case 246:
                    _lastCameraState = false;
                    _suppressCameraOsdUntil = DateTime.UtcNow.AddMilliseconds(2000);
                    SetCameraHardwareState(false);
                    OsdOverlay.ShowCamera(false);
                    break;
                case 248:
                    _lastCameraState = false;
                    _suppressCameraOsdUntil = DateTime.UtcNow.AddMilliseconds(2000);
                    SetCameraHardwareState(false);
                    SetMicrophoneState(false);
                    OsdOverlay.ShowCameraMic(false);
                    break;
                case 24600:
                    _lastCameraState = false;
                    _suppressCameraOsdUntil = DateTime.UtcNow.AddMilliseconds(2000);
                    SetCameraHardwareState(false);
                    OsdOverlay.ShowCamera(false);
                    break;
                case 247:
                    _lastCameraState = true;
                    _suppressCameraOsdUntil = DateTime.UtcNow.AddMilliseconds(2000);
                    SetCameraHardwareState(true);
                    OsdOverlay.ShowCamera(true);
                    break;
                case 249:
                    _lastCameraState = true;
                    _suppressCameraOsdUntil = DateTime.UtcNow.AddMilliseconds(2000);
                    SetCameraHardwareState(true);
                    SetMicrophoneState(true);
                    OsdOverlay.ShowCameraMic(true);
                    break;
                case 24700:
                case 24900:
                    _lastCameraState = true;
                    _suppressCameraOsdUntil = DateTime.UtcNow.AddMilliseconds(2000);
                    SetCameraHardwareState(true);
                    OsdOverlay.ShowCamera(true);
                    break;
                case 303:
                case 304:
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        var win = System.Windows.Application.Current.MainWindow;
                        if (win != null)
                        {
                            win.Show();
                            win.WindowState = System.Windows.WindowState.Normal;
                            win.Activate();
                        }
                    });
                    break;
            }
        }

        public static void QueryKeyboardSettingsFromEC()
        {
            if (Structs.KeyboardEffect.IsAnySoftwareEffectRunning())
            {
                return;
            }
            try
            {
                byte[] input = new byte[256];
                byte[] output = new byte[256];
                input[0] = 24;
                int res = InsydeDriver.SetDCHU_DataEx(4, input, 256, ref output[0]);
                if (res > 0)
                {
                    Array.Copy(output, _lastEcKeyboardState, Math.Min(output.Length, _lastEcKeyboardState.Length));

                    if (output[0] == 1) _isFnLock = true;
                    else if (output[0] == 2) _isFnLock = false;

                    if (output[1] == 2) _isWinKeyLocked = true;
                    else if (output[1] == 1) _isWinKeyLocked = false;

                    if (output[2] != 0)
                    {
                        IsSwapWinFnSupported = true;
                        _isSwapWinFn = (output[2] == 1);
                    }
                    else
                    {
                        IsSwapWinFnSupported = false;
                        _isSwapWinFn = false;
                    }
                }
            }
            catch
            {
            }
        }

        private static int ReadFnLockFromEC()
        {
            try
            {
                byte[] input = new byte[256];
                byte[] output = new byte[256];
                input[0] = 24;
                int res = InsydeDriver.SetDCHU_DataEx(4, input, 256, ref output[0]);
                if (res > 0)
                {
                    Array.Copy(output, _lastEcKeyboardState, Math.Min(output.Length, _lastEcKeyboardState.Length));
                    if (output[0] == 1 || output[0] == 2)
                    {
                        return output[0];
                    }
                }

                byte[] fnState = new byte[1];
                if (InsydeDriver.ReadAppSettings(1, 36, 1, ref fnState[0]) > 0)
                {
                    return fnState[0];
                }
            }
            catch
            {
            }
            return 0;
        }

        public static void ApplyKeyboardSettingsToEC()
        {
            // Writing keyboard settings makes the EC echo a Fn-lock event; ignore it so we do not toggle back.
            _suppressEcKeyboardEventsUntilUtc = DateTime.UtcNow.AddMilliseconds(1500);
            try
            {
                byte[] input = new byte[256];
                byte[] output = new byte[256];
                input[0] = 25;
                input[1] = (byte)(_isFnLock ? 1 : 2);
                input[2] = (byte)(_isWinKeyLocked ? 2 : 1);
                input[3] = IsSwapWinFnSupported ? (byte)(_isSwapWinFn ? 1 : 2) : (byte)0;
                input[4] = _lastEcKeyboardState[3];
                input[5] = _lastEcKeyboardState[4];

                _lastEcKeyboardState[0] = input[1];
                _lastEcKeyboardState[1] = input[2];
                _lastEcKeyboardState[2] = input[3];

                InsydeDriver.SetDCHU_DataEx(4, input, 256, ref output[0]);

                try
                {
                    byte[] kbSettings = new byte[3] { input[1], input[2], input[3] };
                    InsydeDriver.WriteAppSettings(1, 36, 3, ref kbSettings[0]);
                }
                catch
                {
                }
            }
            catch
            {
            }
        }

        public static void ResetKeyboardPreferences()
        {
            _isFnLock = false;
            _isWinKeyLocked = false;
            _isSwapWinFn = false;

            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "FnKeyLock", 0, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "WinKeyLock", 0, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "SwapWinFn", 0, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\ControlCenter2.0", "FnLock", 0, RegistryValueKind.DWord);
            }
            catch
            {
            }

            ApplyKeyboardSettingsToEC();
            UpdateHookState();
        }

        public static bool SetWinKeyLock(bool enable)
        {
            _isWinKeyLocked = enable;
            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "WinKeyLock", enable ? 1 : 0, RegistryValueKind.DWord);
            }
            catch
            {
            }

            ApplyKeyboardSettingsToEC();
            UpdateHookState();
            _lastWinKeyChangeTime = DateTime.UtcNow;
            SendHKeyMessage(262, (byte)(enable ? 1 : 0));
            OsdOverlay.ShowWinKeyLock(_isWinKeyLocked);
            return _isWinKeyLocked;
        }

        public static bool ToggleWinKeyLock()
        {
            return SetWinKeyLock(!_isWinKeyLocked);
        }

        public static bool SetSwapWinFn(bool enable)
        {
            _isSwapWinFn = enable;
            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "SwapWinFn", _isSwapWinFn ? 1 : 0, RegistryValueKind.DWord);
            }
            catch
            {
            }

            ApplyKeyboardSettingsToEC();
            UpdateHookState();
            return _isSwapWinFn;
        }

        public static bool ToggleSwapWinFn()
        {
            return SetSwapWinFn(!_isSwapWinFn);
        }

        public static bool SetFnLock(bool enable)
        {
            _isFnLock = enable;
            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "FnKeyLock", _isFnLock ? 1 : 0, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\ControlCenter2.0", "FnLock", _isFnLock ? 1 : 0);
            }
            catch
            {
            }

            ApplyKeyboardSettingsToEC();
            UpdateHookState();
            OsdOverlay.ShowFnLock(_isFnLock);
            return _isFnLock;
        }

        public static bool ToggleFnLock()
        {
            return SetFnLock(!_isFnLock);
        }

        private static bool ToggleFnLockFromEC()
        {
            int ecState = ReadFnLockFromEC();
            if (ecState == 1)
                return SetFnLock(false);
            else if (ecState == 2)
                return SetFnLock(true);
            return SetFnLock(!_isFnLock);
        }

        public static bool GetCapsLockState()
        {
            return (GetKeyState(VK_CAPITAL) & 1) != 0;
        }

        public static void ToggleCapsLock()
        {
            keybd_event((byte)VK_CAPITAL, 0x45, 0, 0);
            keybd_event((byte)VK_CAPITAL, 0x45, 2, 0);
            SendHKeyMessage(265, 0);
        }

        public static bool GetNumLockState()
        {
            return (GetKeyState(VK_NUMLOCK) & 1) != 0;
        }

        public static void ToggleNumLock()
        {
            keybd_event((byte)VK_NUMLOCK, 0x45, 0, 0);
            keybd_event((byte)VK_NUMLOCK, 0x45, 2, 0);
            SendHKeyMessage(267, 0);
        }

        public static bool GetScrollLockState()
        {
            return (GetKeyState(VK_SCROLL) & 1) != 0;
        }

        public static void ToggleScrollLock()
        {
            keybd_event((byte)VK_SCROLL, 0x45, 0, 0);
            keybd_event((byte)VK_SCROLL, 0x45, 2, 0);
            SendHKeyMessage(266, 0);
        }

        public static bool GetCameraState()
        {
            try
            {
                Guid[] guids = new Guid[] { GUID_DEVCLASS_CAMERA, GUID_DEVCLASS_IMAGE };
                foreach (Guid classGuid in guids)
                {
                    Guid guidCopy = classGuid;
                    IntPtr hDevInfo = SetupDiGetClassDevs(ref guidCopy, null, IntPtr.Zero, 0);
                    if (hDevInfo == IntPtr.Zero || hDevInfo == (IntPtr)(-1))
                        continue;

                    try
                    {
                        SP_DEVINFO_DATA devInfoData = new SP_DEVINFO_DATA();
                        devInfoData.cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                        uint index = 0;

                        while (SetupDiEnumDeviceInfo(hDevInfo, index, ref devInfoData))
                        {
                            index++;
                            uint status, problem;
                            if (CM_Get_DevNode_Status(out status, out problem, devInfoData.DevInst, 0) == 0)
                            {
                                if (problem == CM_PROB_DISABLED || (status & DN_HAS_PROBLEM) != 0)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    finally
                    {
                        SetupDiDestroyDeviceInfoList(hDevInfo);
                    }
                }
            }
            catch
            {
            }

            try
            {
                Guid camGuid = GUID_DEVCLASS_CAMERA;
                int installedCameras = 0;
                int presentCameras = 0;

                IntPtr hInstalled = SetupDiGetClassDevs(ref camGuid, null, IntPtr.Zero, 0);
                if (hInstalled != IntPtr.Zero && hInstalled != (IntPtr)(-1))
                {
                    try
                    {
                        SP_DEVINFO_DATA d = new SP_DEVINFO_DATA { cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA)) };
                        uint idx = 0;
                        while (SetupDiEnumDeviceInfo(hInstalled, idx, ref d))
                        {
                            installedCameras++;
                            idx++;
                        }
                    }
                    finally
                    {
                        SetupDiDestroyDeviceInfoList(hInstalled);
                    }
                }

                IntPtr hPresent = SetupDiGetClassDevs(ref camGuid, null, IntPtr.Zero, DIGCF_PRESENT);
                if (hPresent != IntPtr.Zero && hPresent != (IntPtr)(-1))
                {
                    try
                    {
                        SP_DEVINFO_DATA d = new SP_DEVINFO_DATA { cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA)) };
                        uint idx = 0;
                        while (SetupDiEnumDeviceInfo(hPresent, idx, ref d))
                        {
                            presentCameras++;
                            idx++;
                        }
                    }
                    finally
                    {
                        SetupDiDestroyDeviceInfoList(hPresent);
                    }
                }

                if (installedCameras > 0 && presentCameras == 0)
                {
                    return false;
                }

                object hklmVal = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam", "Value", "Allow");
                if (hklmVal != null && string.Equals(hklmVal.ToString(), "Deny", StringComparison.OrdinalIgnoreCase))
                    return false;

                object hkcuVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam", "Value", "Allow");
                if (hkcuVal != null && string.Equals(hkcuVal.ToString(), "Deny", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            catch
            {
            }

            return true;
        }

        public static bool ToggleCamera()
        {
            bool current = GetCameraState();
            bool target = !current;
            _lastCameraState = target;
            _suppressCameraOsdUntil = DateTime.UtcNow.AddMilliseconds(2500);
            SetCameraHardwareState(target);
            OsdOverlay.ShowCamera(target);
            return target;
        }

        public static void SetDeviceEnabled(Guid classGuid, string[] instanceIdFilters, bool enable)
        {
            try
            {
                Guid guidCopy = classGuid;
                IntPtr hDevInfo = SetupDiGetClassDevs(ref guidCopy, null, IntPtr.Zero, 0);
                if (hDevInfo == IntPtr.Zero || hDevInfo == (IntPtr)(-1))
                    return;

                try
                {
                    SP_DEVINFO_DATA devInfoData = new SP_DEVINFO_DATA();
                    devInfoData.cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                    uint index = 0;

                    while (SetupDiEnumDeviceInfo(hDevInfo, index, ref devInfoData))
                    {
                        index++;

                        System.Text.StringBuilder instanceIdBuf = new System.Text.StringBuilder(512);
                        int reqSize;
                        string instanceId = string.Empty;
                        if (SetupDiGetDeviceInstanceId(hDevInfo, ref devInfoData, instanceIdBuf, instanceIdBuf.Capacity, out reqSize))
                        {
                            instanceId = instanceIdBuf.ToString();
                        }

                        if (instanceIdFilters != null && instanceIdFilters.Length > 0)
                        {
                            bool matches = false;
                            foreach (string filter in instanceIdFilters)
                            {
                                if (!string.IsNullOrEmpty(instanceId) && instanceId.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    matches = true;
                                    break;
                                }
                            }
                            if (!matches) continue;
                        }

                        SP_PROPCHANGE_PARAMS pcp = new SP_PROPCHANGE_PARAMS();
                        pcp.ClassInstallHeader = new SP_CLASSINSTALL_HEADER
                        {
                            cbSize = Marshal.SizeOf(typeof(SP_CLASSINSTALL_HEADER)),
                            InstallFunction = (int)DIF_PROPERTYCHANGE
                        };
                        pcp.StateChange = enable ? (int)DICS_ENABLE : (int)DICS_DISABLE;
                        pcp.Scope = (int)DICS_FLAG_GLOBAL;
                        pcp.HwProfile = 0;

                        int pcpSize = Marshal.SizeOf(typeof(SP_PROPCHANGE_PARAMS));
                        if (!SetupDiSetClassInstallParams(hDevInfo, ref devInfoData, ref pcp, pcpSize))
                        {
                            pcp.Scope = (int)DICS_FLAG_CONFIGSPECIFIC;
                            SetupDiSetClassInstallParams(hDevInfo, ref devInfoData, ref pcp, pcpSize);
                        }

                        SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, hDevInfo, ref devInfoData);

                        if (!string.IsNullOrEmpty(instanceId))
                        {
                            try
                            {
                                ProcessStartInfo psInfo = new ProcessStartInfo
                                {
                                    FileName = "powershell.exe",
                                    Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"" +
                                                (enable ? "Enable-PnpDevice" : "Disable-PnpDevice") +
                                                " -InstanceId '" + instanceId + "' -Confirm:$false -ErrorAction SilentlyContinue\"",
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                    WindowStyle = ProcessWindowStyle.Hidden
                                };
                                Process psProc = Process.Start(psInfo);
                                psProc?.WaitForExit(3000);
                            }
                            catch
                            {
                            }

                            try
                            {
                                ProcessStartInfo psi = new ProcessStartInfo
                                {
                                    FileName = "pnputil.exe",
                                    Arguments = (enable ? "/enable-device \"" : "/disable-device \"") + instanceId + "\"",
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                    WindowStyle = ProcessWindowStyle.Hidden
                                };
                                Process proc = Process.Start(psi);
                                proc?.WaitForExit(2000);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                finally
                {
                    SetupDiDestroyDeviceInfoList(hDevInfo);
                }
            }
            catch
            {
            }
        }

        public static void SetDiscreteGpuState(bool enable)
        {
            try
            {
                string[] discreteGpuFilters = new string[] { "PCI\\VEN_10DE", "PCI\\VEN_1002" };

                if (enable)
                {
                    // Allow EC hardware and PCIe power rail time to stabilize when waking from Power Saver
                    System.Threading.Thread.Sleep(300);

                    // Scan hardware bus first to detect present PCIe devices
                    RunScanDevices();

                    // Enable display adapter node in Device Manager
                    SetDeviceEnabled(GUID_DEVCLASS_DISPLAY, discreteGpuFilters, true);

                    // Second bus scan to ensure display stack and audio sub-devices bind cleanly
                    RunScanDevices();

                    // Re-detect active GPU and re-apply current TGP wattage profile
                    NvidiaPowerControl.ResetDetection();
                    if (NvidiaPowerControl.IsNvidiaPresent)
                    {
                        int tgp = NvidiaPowerControl.GetTgpOffset();
                        NvidiaPowerControl.SetTgpOffset(tgp);
                    }
                }
                else
                {
                    // Disable dGPU to maximize battery runtime in Power Saver mode
                    SetDeviceEnabled(GUID_DEVCLASS_DISPLAY, discreteGpuFilters, false);
                }
            }
            catch
            {
            }
        }

        public static void RestoreDiscreteGpu()
        {
            SetDiscreteGpuState(true);
        }

        private static void RunScanDevices()
        {
            try
            {
                ProcessStartInfo scanPsi = new ProcessStartInfo
                {
                    FileName = "pnputil.exe",
                    Arguments = "/scan-devices",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process proc = Process.Start(scanPsi);
                proc?.WaitForExit(3000);
            }
            catch
            {
            }
        }

        public static void SetCameraHardwareState(bool enable)
        {
            try
            {
                Guid[] guids = new Guid[] { GUID_DEVCLASS_CAMERA, GUID_DEVCLASS_IMAGE };
                foreach (Guid classGuid in guids)
                {
                    SetDeviceEnabled(classGuid, null, enable);
                }
            }
            catch
            {
            }

            string regValue = enable ? "Allow" : "Deny";
            try
            {
                Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam",
                    "Value",
                    regValue,
                    RegistryValueKind.String);
            }
            catch
            {
            }

            try
            {
                Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam",
                    "Value",
                    regValue,
                    RegistryValueKind.String);

                Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam\NonPackaged",
                    "Value",
                    regValue,
                    RegistryValueKind.String);

                using (RegistryKey consentKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam", true))
                {
                    if (consentKey != null)
                    {
                        foreach (string subName in consentKey.GetSubKeyNames())
                        {
                            try
                            {
                                using (RegistryKey subKey = consentKey.OpenSubKey(subName, true))
                                {
                                    subKey?.SetValue("Value", regValue, RegistryValueKind.String);
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            _lastCameraState = enable;
            _suppressCameraOsdUntil = DateTime.UtcNow.AddMilliseconds(2500);

            try
            {
                byte[] camBytes = BitConverter.GetBytes(enable ? 1 : 0);
                InsydeDriver.WriteAppSettings(1, 6, 1, ref camBytes[0]);
                InsydeDriver.SetDCHU_Data(34, camBytes, 4);
            }
            catch
            {
            }

            SendHKeyMessage(257, (byte)(enable ? 1 : 0));
        }

        public static bool GetMicrophoneState()
        {
            try
            {
                var enumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;
                if (enumerator != null)
                {
                    int hr = enumerator.EnumAudioEndpoints(1, 1, out IMMDeviceCollection collection);
                    if (hr == 0 && collection != null)
                    {
                        collection.GetCount(out int count);
                        if (count > 0)
                        {
                            Guid iid = typeof(IAudioEndpointVolume).GUID;
                            for (int i = 0; i < count; i++)
                            {
                                if (collection.Item(i, out IMMDevice device) == 0 && device != null)
                                {
                                    if (device.Activate(ref iid, 23, IntPtr.Zero, out object epObj) == 0 && epObj is IAudioEndpointVolume endpointVolume)
                                    {
                                        if (endpointVolume.GetMute(out bool isMuted) == 0)
                                        {
                                            return !isMuted;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                string val = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone",
                    "Value",
                    null) as string;
                if (!string.IsNullOrEmpty(val))
                {
                    return !string.Equals(val, "Deny", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
            }

            try
            {
                if (true)
                {
                    byte val = 0;
                    if (InsydeDriver.ReadAppSettings(1, 41, 1, ref val) != 0)
                    {
                        return val == 0;
                    }
                }
            }
            catch
            {
            }

            return true;
        }

        public static bool ToggleMicrophone()
        {
            if ((DateTime.UtcNow - _lastMicToggleUtc).TotalMilliseconds < 250)
            {
                return _lastMicState ?? GetMicrophoneState();
            }
            _lastMicToggleUtc = DateTime.UtcNow;
            bool current = GetMicrophoneState();
            bool target = !current;
            SetMicrophoneState(target);
            OsdOverlay.ShowMicrophone(target);
            return target;
        }

        public static void SetMicrophoneState(bool enable)
        {
            _lastMicState = enable;
            _suppressMicOsdUntil = DateTime.UtcNow.AddMilliseconds(2500);

            try
            {
                var enumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator;
                if (enumerator != null)
                {
                    int hr = enumerator.EnumAudioEndpoints(1, 1, out IMMDeviceCollection collection);
                    if (hr == 0 && collection != null)
                    {
                        collection.GetCount(out int count);
                        Guid iid = typeof(IAudioEndpointVolume).GUID;
                        Guid ctx = Guid.Empty;
                        for (int i = 0; i < count; i++)
                        {
                            if (collection.Item(i, out IMMDevice device) == 0 && device != null)
                            {
                                if (device.Activate(ref iid, 23, IntPtr.Zero, out object epObj) == 0 && epObj is IAudioEndpointVolume endpointVolume)
                                {
                                    endpointVolume.SetMute(!enable, ref ctx);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            string regValue = enable ? "Allow" : "Deny";
            try
            {
                Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone",
                    "Value",
                    regValue,
                    RegistryValueKind.String);

                Registry.SetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone\NonPackaged",
                    "Value",
                    regValue,
                    RegistryValueKind.String);
            }
            catch
            {
            }

            try
            {
                Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone",
                    "Value",
                    regValue,
                    RegistryValueKind.String);
            }
            catch
            {
            }

            try
            {
                if (true)
                {
                    byte mutedByte = (byte)(enable ? 0 : 1);
                    InsydeDriver.WriteAppSettings(1, 41, 1, ref mutedByte);
                }
            }
            catch
            {
            }
        }

        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumeratorComObject
        {
        }

        [ComImport]
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            [PreserveSig]
            int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IMMDeviceCollection ppDevices);
        }

        [ComImport]
        [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceCollection
        {
            [PreserveSig]
            int GetCount(out int pcDevices);

            [PreserveSig]
            int Item(int nDevice, out IMMDevice ppDevice);
        }

        [ComImport]
        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig]
            int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        }

        [ComImport]
        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioEndpointVolume
        {
            [PreserveSig]
            int RegisterControlChangeNotify(IntPtr pNotify);
            [PreserveSig]
            int UnregisterControlChangeNotify(IntPtr pNotify);
            [PreserveSig]
            int GetChannelCount(out int pnChannelCount);
            [PreserveSig]
            int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
            [PreserveSig]
            int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
            [PreserveSig]
            int GetMasterVolumeLevel(out float pfLevelDB);
            [PreserveSig]
            int GetMasterVolumeLevelScalar(out float pfLevel);
            [PreserveSig]
            int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
            [PreserveSig]
            int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
            [PreserveSig]
            int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
            [PreserveSig]
            int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
            [PreserveSig]
            int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
            [PreserveSig]
            int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
        }

        public static bool GetAirplaneModeState()
        {
            try
            {
                CoInitialize(IntPtr.Zero);
                try
                {
                    IntPtr irm = IntPtr.Zero;
                    Guid clsid = CLSID_RadioManager;
                    Guid iid = IID_RadioManager;
                    uint hr = CoCreateInstance(ref clsid, IntPtr.Zero, 4, ref iid, out irm);
                    if (hr == 0 && irm != IntPtr.Zero)
                    {
                        IntPtr vtbl = Marshal.ReadIntPtr(irm);
                        IntPtr[] methods = new IntPtr[8];
                        Marshal.Copy(vtbl, methods, 0, 8);

                        var getState = (GetSystemRadioStateDelegate)Marshal.GetDelegateForFunctionPointer(methods[5], typeof(GetSystemRadioStateDelegate));
                        var release = (ReleaseDelegate)Marshal.GetDelegateForFunctionPointer(methods[2], typeof(ReleaseDelegate));

                        int currentState = 0, source = 0, p3 = 0;
                        int res = getState(irm, out currentState, out source, out p3);
                        release(irm);

                        if (res == 0)
                        {
                            // currentState == 1 means radios are enabled (Airplane Mode is OFF)
                            // currentState == 0 means radios are disabled (Airplane Mode is ON)
                            return currentState == 0;
                        }
                    }
                }
                finally
                {
                    CoUninitialize();
                }
            }
            catch
            {
            }
            return false;
        }

        public static bool ToggleAirplaneMode()
        {
            SendHKeyMessage(264, 0);

            bool newActiveState = false;
            bool toggledViaCom = false;
            try
            {
                CoInitialize(IntPtr.Zero);
                try
                {
                    IntPtr irm = IntPtr.Zero;
                    Guid clsid = CLSID_RadioManager;
                    Guid iid = IID_RadioManager;
                    uint hr = CoCreateInstance(ref clsid, IntPtr.Zero, 4, ref iid, out irm);
                    if (hr == 0 && irm != IntPtr.Zero)
                    {
                        IntPtr vtbl = Marshal.ReadIntPtr(irm);
                        IntPtr[] methods = new IntPtr[8];
                        Marshal.Copy(vtbl, methods, 0, 8);

                        var getState = (GetSystemRadioStateDelegate)Marshal.GetDelegateForFunctionPointer(methods[5], typeof(GetSystemRadioStateDelegate));
                        var setState = (SetSystemRadioStateDelegate)Marshal.GetDelegateForFunctionPointer(methods[6], typeof(SetSystemRadioStateDelegate));
                        var release = (ReleaseDelegate)Marshal.GetDelegateForFunctionPointer(methods[2], typeof(ReleaseDelegate));

                        int currentState = 0, source = 0, p3 = 0;
                        if (getState(irm, out currentState, out source, out p3) == 0)
                        {
                            // currentState == 1: radios enabled (Airplane Mode OFF). Target is 0 (disable radios -> Airplane Mode ON).
                            // currentState == 0: radios disabled (Airplane Mode ON). Target is 1 (enable radios -> Airplane Mode OFF).
                            int targetState = (currentState == 0) ? 1 : 0;
                            setState(irm, targetState);
                            newActiveState = (targetState == 0);
                            toggledViaCom = true;
                        }

                        release(irm);
                    }
                }
                finally
                {
                    CoUninitialize();
                }
            }
            catch
            {
            }

            if (!toggledViaCom)
            {
                keybd_event(VK_AIRPLANE, 0x47, 0, 0);
                keybd_event(VK_AIRPLANE, 0x47, 2, 0);
            }

            _lastAirplaneState = newActiveState;
            _suppressAirplaneOsdUntil = DateTime.UtcNow.AddMilliseconds(2000);
            OsdOverlay.ShowAirplane(newActiveState);
            return newActiveState;
        }

        public static void UpdateHookState()
        {
            if (true || _isWinKeyLocked || _isFnLock || OsdOverlay.IsEnabled)
            {
                InstallKeyboardHook();
            }
            else
            {
                RemoveKeyboardHook();
            }
        }

        private static void InstallKeyboardHook()
        {
            if (_hookId != IntPtr.Zero) return;
            try
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
            catch
            {
            }
        }

        private static void RemoveKeyboardHook()
        {
            if (_hookId != IntPtr.Zero)
            {
                try
                {
                    UnhookWindowsHookEx(_hookId);
                }
                catch
                {
                }
                _hookId = IntPtr.Zero;
            }
        }

        private static IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT hookStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

                // Ignore synthetic keys to prevent hook recursion and self-blocking
                const int LLKHF_INJECTED = 0x10;
                if ((hookStruct.flags & LLKHF_INJECTED) != 0)
                {
                    return CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                int msg = (int)wParam;
                bool isKeyDown = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN);
                bool isKeyUp = (msg == WM_KEYUP || msg == WM_SYSKEYUP);
                uint vk = hookStruct.vkCode;

                if (vk == VK_LWIN || vk == VK_RWIN)
                {
                    if (isKeyDown) _hookWinDown = true;
                    else if (isKeyUp) _hookWinDown = false;
                }
                else if (vk == 0x12 || vk == 0xA4 || vk == 0xA5)
                {
                    if (isKeyDown) _hookAltDown = true;
                    else if (isKeyUp) _hookAltDown = false;
                }

                bool winKeyActive = _hookWinDown
                    || (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0
                    || (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0
                    || (GetKeyState(VK_LWIN) & 0x8000) != 0
                    || (GetKeyState(VK_RWIN) & 0x8000) != 0;
                bool altKeyActive = _hookAltDown
                    || (hookStruct.flags & 0x20) != 0
                    || (GetAsyncKeyState(0x12) & 0x8000) != 0
                    || (GetAsyncKeyState(0xA4) & 0x8000) != 0
                    || (GetAsyncKeyState(0xA5) & 0x8000) != 0
                    || (GetKeyState(0x12) & 0x8000) != 0;

                if (vk == 0x4B && (winKeyActive && altKeyActive || _handledFnKeys.Contains(0x4B)))
                {
                    if (isKeyDown)
                    {
                        _handledFnKeys.Add(0x4B);
                        keybd_event(0xFF, 0, 2, 0);
                        System.Threading.Tasks.Task.Run(() => ToggleMicrophone());
                    }
                    else if (isKeyUp)
                    {
                        _handledFnKeys.Remove(0x4B);
                    }
                    return (IntPtr)1;
                }

                if (_isWinKeyLocked && (vk == VK_LWIN || vk == VK_RWIN))
                {
                    if (isKeyDown && OsdOverlay.IsEnabled)
                    {
                        if (DateTime.UtcNow >= _lastWinKeyOsdTime.AddMilliseconds(500))
                        {
                            _lastWinKeyOsdTime = DateTime.UtcNow;
                            OsdOverlay.ShowWinKeyLock(true);
                        }
                    }
                    return (IntPtr)1;
                }

                // Intercept Backlight keys when Win/Fn is active to prevent Windows Magnifier (Win + + / Win + -)
                if (winKeyActive)
                {
                    if (vk == 0xBB || vk == 0x6B) // + / = or NumPad +: increase brightness
                    {
                        if (isKeyDown)
                        {
                            int? step = KeyboardBackLight.IncreaseBrightness();
                            if (step.HasValue)
                            {
                                OsdOverlay.ShowBacklight(step.Value);
                            }
                        }
                        return (IntPtr)1; // Consume key: prevents Win + + from launching Magnifier
                    }
                    if (vk == 0xBD || vk == 0x6D) // - or NumPad -: decrease brightness
                    {
                        if (isKeyDown)
                        {
                            int? step = KeyboardBackLight.DecreaseBrightness();
                            if (step.HasValue)
                            {
                                OsdOverlay.ShowBacklight(step.Value);
                            }
                        }
                        return (IntPtr)1; // Consume key: prevents Win + -
                    }
                    if (vk == 0x6A || vk == 0x38) // * or NumPad * (8): toggle backlight
                    {
                        if (isKeyDown)
                        {
                            OsdOverlay.ShowBacklightToggle(KeyboardBackLight.ToggleBacklight());
                        }
                        return (IntPtr)1;
                    }
                    if (vk == 0xBF || vk == 0x6F || vk == 0x20) // / or NumPad / or Space: cycle color
                    {
                        if (isKeyDown)
                        {
                            var preset = KeyboardBackLight.CycleColorPreset();
                            if (preset.HasValue)
                            {
                                OsdOverlay.ShowBacklightColor(preset.Value.Name);
                            }
                        }
                        return (IntPtr)1;
                    }
                }

                if (vk == 0x87)
                {
                    _lastHardwareTouchpadKeyUtc = DateTime.UtcNow;
                }

                if (OsdOverlay.IsEnabled && isKeyUp)
                {
                    if (vk == VK_CAPITAL)
                    {
                        OsdOverlay.ShowCaps(GetCapsLockState());
                    }
                    else if (vk == VK_NUMLOCK)
                    {
                        OsdOverlay.ShowNum(GetNumLockState());
                    }
                    else if (vk == VK_SCROLL)
                    {
                        OsdOverlay.ShowScroll(GetScrollLockState());
                    }
                    else if (vk == VK_AIRPLANE || vk == 0xF2)
                    {
                        System.Threading.Tasks.Task.Delay(120).ContinueWith(_ =>
                        {
                            bool airState = GetAirplaneModeState();
                            _lastAirplaneState = airState;
                            _suppressAirplaneOsdUntil = DateTime.UtcNow.AddMilliseconds(2000);
                            OsdOverlay.ShowAirplane(airState);
                        });
                    }
                    else if (vk == 0x87)
                    {
                        System.Threading.Tasks.Task.Delay(60).ContinueWith(_ =>
                        {
                            bool state = Touchpad.GetTouchpadState();
                            Touchpad.SyncTouchpadToEC(state);
                            OsdOverlay.ShowTouchpad(state);
                        });
                    }
                }

                if (_isFnLock && _lastEcKeyboardState[0] == 0 && IsFnKey(vk))
                {
                    // Pass-through guard: allow OS shortcuts like Alt+F4, Ctrl+F5, Shift+F10 through normally
                    bool ctrlDown = (GetKeyState(0x11) & 0x8000) != 0;
                    bool shiftDown = (GetKeyState(0x10) & 0x8000) != 0;
                    bool altDown = (GetKeyState(0x12) & 0x8000) != 0;
                    if (ctrlDown || altDown || shiftDown)
                    {
                        return CallNextHookEx(_hookId, nCode, wParam, lParam);
                    }

                    if (isKeyDown)
                    {
                        if (HandleFnAction(vk))
                        {
                            _handledFnKeys.Add(vk);
                            return (IntPtr)1;
                        }
                    }
                    else if (isKeyUp)
                    {
                        if (_handledFnKeys.Remove(vk))
                        {
                            return (IntPtr)1;
                        }
                    }
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static bool IsFnKey(uint vk)
        {
            return vk >= 0x70 && vk <= 0x7B;
        }

        private static bool HandleFnAction(uint vk)
        {
            switch (vk)
            {
                case 0x70: // F1: Touchpad Toggle
                    bool tp = Touchpad.ToggleTouchpad();
                    OsdOverlay.ShowTouchpad(tp);
                    return true;
                case 0x71: // F2: Turn off LCD Display
                    PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, MONITOR_OFF);
                    return true;
                case 0x72: // F3: Mute Audio
                    keybd_event(VK_VOLUME_MUTE, 0, 0, 0);
                    keybd_event(VK_VOLUME_MUTE, 0, 2, 0);
                    return true;
                case 0x73: // F4: Cycle Keyboard Backlight
                    CycleKeyboardBacklight();
                    return true;
                case 0x74: // F5: Volume Down
                    keybd_event(VK_VOLUME_DOWN, 0, 0, 0);
                    keybd_event(VK_VOLUME_DOWN, 0, 2, 0);
                    return true;
                case 0x75: // F6: Volume Up
                    keybd_event(VK_VOLUME_UP, 0, 0, 0);
                    keybd_event(VK_VOLUME_UP, 0, 2, 0);
                    return true;
                case 0x76: // F7: Display Projection (Win + P)
                    keybd_event((byte)VK_LWIN, 0, 0, 0);
                    keybd_event(0x50, 0, 0, 0);
                    keybd_event(0x50, 0, 2, 0);
                    keybd_event((byte)VK_LWIN, 0, 2, 0);
                    return true;
                case 0x77: // F8: Brightness Down
                    AdjustScreenBrightness(-10);
                    return true;
                case 0x78: // F9: Brightness Up
                    AdjustScreenBrightness(10);
                    return true;
                case 0x79: // F10: Camera Toggle
                    ToggleCamera();
                    return true;
                case 0x7A: // F11: Airplane Mode Toggle
                    bool air = ToggleAirplaneMode();
                    return true;
                case 0x7B: // F12: Sleep
                    SetSuspendState(false, false, false);
                    return true;
            }
            return false;
        }

        public static void AdjustScreenBrightness(int delta)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT CurrentBrightness FROM WmiMonitorBrightness"))
                {
                    int current = 50;
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        current = Convert.ToInt32(obj["CurrentBrightness"]);
                        break;
                    }

                    int target = Math.Max(0, Math.Min(100, current + delta));
                    using (var methodSearcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM WmiMonitorBrightnessMethods"))
                    {
                        foreach (ManagementObject obj in methodSearcher.Get())
                        {
                            obj.InvokeMethod("WmiSetBrightness", new object[] { 1, (byte)target });
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public static void CycleKeyboardBacklight()
        {
            try
            {
                int nextLevel = KeyboardBackLight.CycleBrightness();
                OsdOverlay.ShowBacklight(nextLevel);
            }
            catch
            {
            }
        }

        internal static void SendHKeyMessage(int cmd, byte param)
        {
            try
            {
                IntPtr hWnd = FindWindow(null, "HkeyTray");
                if (hWnd != IntPtr.Zero)
                {
                    byte[] data = new byte[5] { (byte)(cmd & 255), param, 0, 0, 0 };
                    GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                    try
                    {
                        COPYDATASTRUCT cds = new COPYDATASTRUCT
                        {
                            dwData = (IntPtr)((cmd >> 8) & 255),
                            cbData = data.Length,
                            lpData = handle.AddrOfPinnedObject()
                        };
                        SendMessageData(hWnd, 74, 2, ref cds);
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
            }
            catch
            {
            }
        }

        public static void ApplyFactoryDefaults()
        {
            try
            {
                FanInfo defaultCpu = new FanInfo(40, 35, 60, 50, 80, 85, 100, 100);
                FanInfo defaultGpu = new FanInfo(40, 35, 60, 50, 80, 85, 100, 100);
                Fan.SetCustomFanTable(defaultCpu, defaultGpu);
                Fan.SetOffset(0);
                Fan.SetMode(Fan.Mode.Auto);

                PowerMode.SetPowerMode(PowerMode.Mode.Entertainment);

                KeyboardEffect.StopAllSoftwareEffects();
                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "SoftwareEffects", 0, RegistryValueKind.DWord);
                }
                catch
                {
                }

                KeyboardBackLight.TurnOn();
                KeyboardBackLight.SetBrightness(255);

                KeyboardHardwareCapabilities caps = KeyboardBackLight.GetCapabilities();
                if (caps.SupportsRgb)
                {
                    KeyboardBackLight.SetColour(0, 0, 255); // Default Blue (#FF0000FF)
                }

                KeyboardBackLight.SetSleepTimer(30);

                if (caps.SupportsBootEffectOverride)
                {
                    KeyboardBackLight.OverrideBootEffect(false);
                }

                if (!Touchpad.GetTouchpadState())
                {
                    Touchpad.ToggleTouchpad();
                }

                ResetKeyboardPreferences();
                BatteryControl.SetChargeMode(BatteryChargeMode.Maximum);

                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "DefaultsApplied", 1, RegistryValueKind.DWord);
                }
                catch
                {
                }
            }
            catch
            {
            }
        }

        public static void ApplySavedSettings()
        {
            try
            {
                PowerMode.Mode savedPower = PowerMode.GetStatus();
                PowerMode.SetPowerMode(savedPower);

                FanStatus fanStatus = Fan.GetStatus();
                Fan.SetOffset((byte)Math.Max(0, Math.Min(100, fanStatus.Offset)));
                if (fanStatus.Mode == Fan.Mode.Custom)
                {
                    Fan.SetCustomFanTable(fanStatus.Fan_CPU, fanStatus.Fan_GPU1);
                }
                Fan.SetMode(fanStatus.Mode);

                KeyboardStatus kbStatus = KeyboardBackLight.GetStatus();
                KeyboardHardwareCapabilities caps = KeyboardBackLight.GetCapabilities();
                byte targetBrightness = kbStatus.BackLight.A > 0 ? kbStatus.BackLight.A : (byte)255;
                KeyboardEffects.MaxBrightness = targetBrightness;

                KeyboardEffect savedEffect = null;
                int animSpeed = 2;
                object regEffectActive = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectActive", null);
                if (regEffectActive is int intEffActive && intEffActive == 1)
                {
                    object regEffectId = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectId", null);
                    object regEffectUniq = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectUniqId", null);
                    object regEffectName = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectName", null);
                    if (regEffectId is int effId)
                    {
                        byte effUniq = regEffectUniq is int u ? (byte)u : (byte)0;
                        string effName = regEffectName as string;
                        savedEffect = KeyboardEffect.LookUpEffect(effId, effUniq, effName);
                    }
                    object regSpeed = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbAnimationSpeed", null);
                    if (regSpeed is int s && s >= 1 && s <= 3) animSpeed = s;
                }

                if (kbStatus.LightState)
                {
                    bool onBattery = KeyboardEffects.IsRunningOnBattery;
                    bool shouldStartEffect = savedEffect != null && (!savedEffect.IsSoftware || !onBattery);

                    if (shouldStartEffect)
                    {
                        KeyboardEffect.CurrentAnimationSpeed = animSpeed;
                        KeyboardEffects.Duration = animSpeed == 1 ? 5000 : (animSpeed == 3 ? 1000 : 2500);
                        KeyboardEffects.MaxBrightness = targetBrightness;

                        if (savedEffect.IsSoftware)
                        {
                            bool isBreath = savedEffect.Id == 8 || savedEffect.Id == 10;
                            if (savedEffect.Id == 8 && caps.SupportsRgb)
                            {
                                KeyboardBackLight.SetColourRealtime(kbStatus.BackLight.R, kbStatus.BackLight.G, kbStatus.BackLight.B);
                            }
                            else if ((savedEffect.Id == 9 || savedEffect.Id == 10) && caps.SupportsRgb)
                            {
                                // Initialize at first Rainbow color instead of flashing static color
                                KeyboardBackLight.SetColourRealtime(255, 0, 0);
                            }

                            KeyboardBackLight.TurnOn(isBreath ? (byte)0 : targetBrightness);

                            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                            {
                                savedEffect.SetEffect();
                            }));
                        }
                        else
                        {
                            KeyboardBackLight.TurnOn(targetBrightness);
                            KeyboardBackLight.SetMode(savedEffect, targetBrightness);
                        }
                    }
                    else
                    {
                        if (savedEffect != null && savedEffect.IsSoftware && onBattery)
                        {
                            KeyboardEffects.SavedBatterySoftwareEffect = savedEffect;
                        }

                        if (caps.SupportsRgb)
                        {
                            KeyboardBackLight.SetColourRealtime(kbStatus.BackLight.R, kbStatus.BackLight.G, kbStatus.BackLight.B);
                        }
                        KeyboardBackLight.TurnOn(targetBrightness);
                        if (caps.SupportsRgb)
                        {
                            KeyboardBackLight.SetColour(kbStatus.BackLight.R, kbStatus.BackLight.G, kbStatus.BackLight.B);
                        }
                        KeyboardBackLight.SetBrightness(targetBrightness);
                    }
                }
                else
                {
                    KeyboardBackLight.TurnOff();
                }
                KeyboardBackLight.SetSleepTimer(kbStatus.SleepSecond);

                ApplyKeyboardSettingsToEC();

                object regBatMode = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "BatteryMode", null);
                if (regBatMode is int intBatMode)
                {
                    int start = 90;
                    int stop = 100;
                    object regStart = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "BatteryStart", 90);
                    object regStop = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "BatteryStop", 100);
                    if (regStart is int s) start = s;
                    if (regStop is int st) stop = st;
                    BatteryControl.SetChargeMode((BatteryChargeMode)intBatMode, start, stop);
                }
                else
                {
                    BatteryChargeStatus batStatus = BatteryControl.GetStatus();
                    BatteryControl.SetChargeMode(batStatus.Mode, batStatus.StartThreshold, batStatus.StopThreshold);
                }
            }
            catch
            {
            }
        }

        public static void InitializeHardwareOnStartup()
        {
            try
            {
                object appliedVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "DefaultsApplied", null);
                bool defaultsApplied = appliedVal is int intVal && intVal == 1;

                if (!defaultsApplied)
                {
                    ApplyFactoryDefaults();
                }
                else
                {
                    ApplySavedSettings();
                }
            }
            catch
            {
            }
        }
    }
}
