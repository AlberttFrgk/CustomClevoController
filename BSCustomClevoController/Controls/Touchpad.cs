using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using BSCustomClevoController.Utility;


namespace BSCustomClevoController.Controls
{
    public static class Touchpad
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_LWIN = 0x5B;
        private const byte VK_F24 = 0x87;

        public static void Initialize()
        {
            try
            {
                bool state = GetTouchpadState();
                SyncTouchpadToEC(state);
            }
            catch
            {
            }
        }

        public static void SyncTouchpadToEC(bool enable)
        {
            try
            {
                byte[] state = new byte[4] { (byte)(enable ? 1 : 0), 0, 0, 0 };
                InsydeDriver.WriteAppSettings(1, 10, 1, ref state[0]);
            }
            catch
            {
            }
        }

        public static bool GetTouchpadState()
        {
            try
            {
                int ecState = 0;
                int ret = InsydeDriver.GetDCHU_Data_Integer(9, ref ecState);
                if (ret >= 0)
                {
                    return ecState == 1;
                }

                object appVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "TouchpadEnabled", null);
                if (appVal != null)
                {
                    return Convert.ToInt32(appVal) != 0;
                }
            }
            catch
            {
            }
            return true;
        }

        private static void SendPrecisionTouchpadShortcut()
        {
            keybd_event(VK_CONTROL, 0, 0, 0);
            keybd_event(VK_LWIN, 0, KEYEVENTF_EXTENDEDKEY, 0);
            keybd_event(VK_F24, 0, 0, 0);
            keybd_event(VK_F24, 0, KEYEVENTF_KEYUP, 0);
            keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP | KEYEVENTF_EXTENDEDKEY, 0);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
        }

        public static DateTime SuppressHardwareEventsUntilUtc = DateTime.MinValue;
        private static DateTime _lastTouchpadToggleUtc = DateTime.MinValue;
        private static readonly object _touchpadToggleLock = new object();

        public static bool SetTouchpadState(bool enable, bool showOsd = true)
        {
            lock (_touchpadToggleLock)
            {
                SuppressHardwareEventsUntilUtc = DateTime.UtcNow.AddMilliseconds(2000);
                _lastTouchpadToggleUtc = DateTime.UtcNow;

                bool current = GetTouchpadState();
                if (current != enable)
                {
                    try
                    {
                        var dispatcher = System.Windows.Application.Current?.Dispatcher;
                        if (dispatcher != null && !dispatcher.CheckAccess())
                        {
                            dispatcher.Invoke(SendPrecisionTouchpadShortcut);
                        }
                        else
                        {
                            SendPrecisionTouchpadShortcut();
                        }
                    }
                    catch
                    {
                    }
                }

                SyncTouchpadToEC(enable);

                try
                {
                    byte[] buffer = BitConverter.GetBytes(enable ? 1u : 0u);
                    InsydeDriver.SetDCHU_Data(42, buffer, 4);
                }
                catch
                {
                }

                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "TouchpadEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
                }
                catch
                {
                }

                if (showOsd)
                {
                    OsdOverlay.ShowTouchpad(enable);
                }
                return enable;
            }
        }

        public static void HandleHardwareStateChange(bool enable)
        {
            lock (_touchpadToggleLock)
            {
                if (DateTime.UtcNow < SuppressHardwareEventsUntilUtc)
                {
                    return;
                }

                SuppressHardwareEventsUntilUtc = DateTime.UtcNow.AddMilliseconds(1500);
                _lastTouchpadToggleUtc = DateTime.UtcNow;

                SyncTouchpadToEC(enable);

                try
                {
                    byte[] buffer = BitConverter.GetBytes(enable ? 1u : 0u);
                    InsydeDriver.SetDCHU_Data(42, buffer, 4);
                }
                catch
                {
                }

                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "TouchpadEnabled", enable ? 1 : 0, RegistryValueKind.DWord);
                }
                catch
                {
                }

                OsdOverlay.ShowTouchpad(enable);
            }
        }

        public static bool ToggleTouchpad()
        {
            lock (_touchpadToggleLock)
            {
                if ((DateTime.UtcNow - _lastTouchpadToggleUtc).TotalMilliseconds < 600)
                {
                    return GetTouchpadState();
                }
                _lastTouchpadToggleUtc = DateTime.UtcNow;
                bool current = GetTouchpadState();
                return SetTouchpadState(!current);
            }
        }
    }
}
