using BSCustomClevoController.Views;
using Microsoft.Win32;
using System;
using System.Windows;
using BSCustomClevoController.Controls;


namespace BSCustomClevoController.Utility
{
    public static class OsdOverlay
    {
        private static OsdWindow _window;
        private static bool _isEnabled = true;
        private static readonly object _lock = new object();
        private static string _lastOsdKey;
        private static DateTime _lastOsdTime = DateTime.MinValue;

        static OsdOverlay()
        {
            try
            {
                object val = Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "ShowOSD", 1);
                if (val is int intVal)
                {
                    _isEnabled = (intVal != 0);
                }
            }
            catch
            {
                _isEnabled = true;
            }
        }

        public static bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                try
                {
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "ShowOSD", value ? 1 : 0, RegistryValueKind.DWord);
                }
                catch
                {
                }
            }
        }

        private static void ShowInternal(string imageName, string text)
        {
            if (!_isEnabled) return;

            string key = imageName + "|" + text;
            lock (_lock)
            {
                if (string.Equals(key, _lastOsdKey, StringComparison.Ordinal) &&
                    DateTime.UtcNow < _lastOsdTime.AddMilliseconds(1000))
                {
                    return;
                }
                _lastOsdKey = key;
                _lastOsdTime = DateTime.UtcNow;
            }

            Application app = Application.Current;
            if (app == null) return;

            app.Dispatcher.BeginInvoke(new Action(() =>
            {
                lock (_lock)
                {
                    if (_window == null)
                    {
                        _window = new OsdWindow();
                        _window.Closed += (s, e) =>
                        {
                            lock (_lock)
                            {
                                _window = null;
                            }
                        };
                    }
                }
                _window.Display(imageName, text);
            }));
        }

        public static void ShowCaps(bool on)
        {
            ShowInternal(on ? "osd_caps_on_cc40_1.png" : "osd_caps_off_cc40_1.png", on ? "CAPS LOCK ON" : "CAPS LOCK OFF");
        }

        public static void ShowNum(bool on)
        {
            ShowInternal(on ? "osd_num_on_cc40_1.png" : "osd_num_off_cc40_1.png", on ? "NUM LOCK ON" : "NUM LOCK OFF");
        }

        public static void ShowScroll(bool on)
        {
            ShowInternal(on ? "osd_scr_on_cc40_1.png" : "osd_scr_off_cc40_1.png", on ? "SCROLL LOCK ON" : "SCROLL LOCK OFF");
        }

        public static void ShowWinKeyLock(bool locked)
        {
            ShowInternal(locked ? "osd_winkey_lock_cc40_1.png" : "osd_winkey_unlock_cc40_1.png", locked ? "WIN KEY LOCKED" : "WIN KEY UNLOCKED");
        }

        public static void ShowTouchpad(bool enabled)
        {
            ShowInternal(enabled ? "osd_touchpad_on_cc40_1.png" : "osd_touchpad_off_cc40_1.png", enabled ? "TOUCHPAD ON" : "TOUCHPAD OFF");
        }

        public static void ShowCamera(bool enabled)
        {
            ShowInternal(enabled ? "osd_camera_on_cc40_1.png" : "osd_camera_off_cc40_1.png", enabled ? "CAMERA ON" : "CAMERA OFF");
        }

        public static void ShowMicrophone(bool enabled)
        {
            ShowInternal(enabled ? "Thirdwave_MicUnmute_cc40_1.png" : "Thirdwave_MicMute_cc40_1.png", enabled ? "MICROPHONE ON" : "MICROPHONE OFF");
        }

        public static void ShowCameraMic(bool enabled)
        {
            ShowInternal(enabled ? "osd_cameraMic_on_cc40_1.png" : "osd_cameraMic_off_cc40_1.png", enabled ? "CAMERA & MIC ON" : "CAMERA & MIC OFF");
        }

        public static void ShowAirplane(bool enabled)
        {
            ShowInternal(enabled ? "osd_airplane_on_cc40_1.png" : "osd_airplane_off_cc40_1.png", enabled ? "AIRPLANE MODE ON" : "AIRPLANE MODE OFF");
        }

        public static void ShowFnLock(bool locked)
        {
            ShowInternal(locked ? "osd_Fn_lock_enable.png" : "osd_Fn_lock_disable.png", locked ? "FN LOCK ON" : "FN LOCK OFF");
        }

        public static void ShowBacklight(int level)
        {
            if (level <= 0)
            {
                ShowBacklightToggle(false);
            }
            else
            {
                int imgIdx = Math.Min(4, Math.Max(1, level));
                ShowInternal(string.Format("osd_backlight_{0}_cc40_1.png", imgIdx), $"KEYBOARD\nBACKLIGHT {imgIdx * 25}%");
            }
        }

        public static void ShowBacklightToggle(bool isOn)
        {
            ShowInternal(
                isOn ? "osd_backlight_on_cc40_1.png" : "osd_backlight_off_cc40_1.png",
                isOn ? "KEYBOARD\nBACKLIGHT ON" : "KEYBOARD\nBACKLIGHT OFF");
        }

        public static void ShowBacklightColor(string colorName)
        {
            ShowInternal("osd_backlight_on_cc40_1.png", $"KEYBOARD\nCOLOR {colorName.ToUpper()}");
        }

        public static void ShowBacklightStep(bool isHigh)
        {
            ShowInternal(
                isHigh ? "osd_backlight_high_cc40_1.png" : "osd_backlight_low_cc40_1.png",
                "KEYBOARD\nBACKLIGHT");
        }

        public static void ShowFan(bool isMax)
        {
            ShowFan(isMax ? Fan.Mode.Max : Fan.Mode.Auto);
        }

        internal static void ShowFan(Fan.Mode mode)
        {
            switch (mode)
            {
                case Fan.Mode.Max:
                    ShowInternal("osd_fan_speed_max_cc40_1.png", "MAX FAN (100%)");
                    break;
                case Fan.Mode.MaxQ:
                    ShowInternal("osd_fan_speed_MaxQ_cc40_1.png", "QUIET FAN");
                    break;
                case Fan.Mode.Custom:
                    ShowInternal("osd_fan_speed_Custom_cc40_1.png", "CUSTOM FAN");
                    break;
                case Fan.Mode.Auto:
                default:
                    ShowInternal("osd_fan_speed_auto_cc40_1.png", "AUTO FAN");
                    break;
            }
        }

        public static void ShowFanAntiDust()
        {
            ShowInternal("osd_fan_speed_AntiDustRuning_cc40_1.png", "ANTI-DUST FAN");
        }

        public static void ShowPowerMode(PowerMode.Mode mode)
        {
            string img;
            string text;
            switch (mode)
            {
                case PowerMode.Mode.Quiet:
                    img = "osd_quiet2_cc40_1.png";
                    text = "QUIET";
                    break;
                case PowerMode.Mode.Powersaving:
                    img = "osd_powerSaving2_cc40_1.png";
                    text = "POWER SAVER";
                    break;
                case PowerMode.Mode.Performance:
                    img = "osd_peformance2_cc40_1.png";
                    text = "PERFORMANCE";
                    break;
                case PowerMode.Mode.Entertainment:
                default:
                    img = "osd_entetainment2_cc40_1.png";
                    text = "ENTERTAINMENT";
                    break;
            }
            ShowInternal(img, text);
        }
    }
}
