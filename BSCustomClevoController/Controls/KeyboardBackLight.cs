using System;
using System.Drawing;
using System.Runtime.InteropServices;
using static BSCustomClevoController.Utility.Structs;
using BSCustomClevoController.Utility;


namespace BSCustomClevoController.Controls
{
    class KeyboardBackLight
    {
        private static int SetDCHU_Data(int command, byte[] buffer, int length)
        {
            return InsydeDriver.SetDCHU_Data(command, buffer, length);
        }

        public static int GetDCHU_Data_Integer(int command, ref int data)
        {
            return InsydeDriver.GetDCHU_Data_Integer(command, ref data);
        }

        private static int WriteAppSettings(int page, int offset, int length, ref byte buffer)
        {
            return InsydeDriver.WriteAppSettings(page, offset, length, ref buffer);
        }

        public static int ReadAppSettings(int page, int offset, int length, ref byte buffer)
        {
            return InsydeDriver.ReadAppSettings(page, offset, length, ref buffer);
        }

        public static KeyboardHardwareCapabilities GetCapabilities()
        {
            var caps = new KeyboardHardwareCapabilities();
            try
            {
                int cmd16 = 0;
                GetDCHU_Data_Integer(16, ref cmd16);

                int cmd96 = 0;
                GetDCHU_Data_Integer(96, ref cmd96);

                int cmd52 = 0;
                GetDCHU_Data_Integer(0x52, ref cmd52);

                int cmd7A = 0;
                GetDCHU_Data_Integer(0x7A, ref cmd7A);

                byte rawType = 2;
                ReadAppSettings(2, 4, 1, ref rawType);

                caps.RawKbType = rawType;

                bool isWhiteOnly = (cmd52 & 0x40000000) != 0;
                bool is3ZoneRgb = (cmd52 & 0x00400000) != 0;
                bool isWhite5Levels = (cmd7A & 0x4000) != 0;
                bool noSupportHw = (cmd96 & 1) == 1;
                bool hasPerKeyBit = (cmd16 & 8) == 8;
                bool hasRgbBit = (cmd16 & 16) == 16;

                caps.SupportsIteDirectHid = IteHidController.IsDevicePresent;
                caps.SupportsLightBar = IteHidController.IsLightBarPresent;

                if (rawType == 0)
                {
                    caps.SupportsBacklight = true;
                    caps.SupportsRgb = true;
                    caps.SupportsHardwareEffects = false;
                    caps.SupportsMultiZone = false;
                    caps.SupportsBootEffectOverride = true;
                    caps.KeyboardTypeName = "Standard Backlight";
                    return caps;
                }

                if (isWhiteOnly || rawType == 1 || (!hasRgbBit && !hasPerKeyBit && !is3ZoneRgb && rawType != 2 && rawType != 3 && rawType != 6 && rawType != 22))
                {
                    // Single-color White LED keyboard
                    caps.SupportsBacklight = true;
                    caps.SupportsRgb = false;
                    caps.SupportsHardwareEffects = false;
                    caps.SupportsMultiZone = false;
                    caps.SupportsBootEffectOverride = true;
                    caps.MaxBrightnessLevels = isWhite5Levels ? 5 : 2;
                    caps.KeyboardTypeName = isWhite5Levels ? "Monochrome White LED (5-Level)" : "Monochrome White LED (2-Level)";
                    return caps;
                }

                caps.SupportsBacklight = true;
                caps.SupportsRgb = true;
                caps.SupportsBootEffectOverride = true;

                if (rawType == 6 || rawType == 22)
                {
                    // Single-zone whole keyboard RGB
                    caps.SupportsMultiZone = false;
                    caps.SupportsHardwareEffects = false;
                    caps.KeyboardTypeName = "Single-Zone RGB";
                }
                else if (is3ZoneRgb)
                {
                    caps.SupportsMultiZone = true;
                    caps.SupportsHardwareEffects = !noSupportHw;
                    caps.KeyboardTypeName = "3-Zone RGB";
                }
                else if (rawType == 3 || hasPerKeyBit)
                {
                    caps.SupportsMultiZone = true;
                    caps.SupportsHardwareEffects = !noSupportHw;
                    caps.KeyboardTypeName = "Per-Key RGB";
                }
                else
                {
                    caps.SupportsMultiZone = true;
                    caps.SupportsHardwareEffects = !noSupportHw;
                    caps.KeyboardTypeName = "Multi-Zone RGB";
                }
            }
            catch
            {
                caps.SupportsBacklight = true;
                caps.SupportsRgb = true;
                caps.SupportsHardwareEffects = true;
                caps.SupportsMultiZone = true;
                caps.SupportsBootEffectOverride = true;
                caps.SupportsIteDirectHid = IteHidController.IsDevicePresent;
                caps.SupportsLightBar = IteHidController.IsLightBarPresent;
                caps.KeyboardTypeName = "Standard RGB";
            }

            return caps;
        }

        public static KeyboardStatus GetStatus()
        {
            try
            {
                byte rawBrightness = 4;
                ReadAppSettings(2, 35, 1, ref rawBrightness);
                byte[] colour = new byte[3] { 0, 0, 255 }; // Default Blue (#FF0000FF)
                ReadAppSettings(2, 81, 3, ref colour[0]);
                byte mode = 0;
                ReadAppSettings(2, 32, 1, ref mode);
                byte status = 1;
                ReadAppSettings(2, 84, 1, ref status);
                byte booteffect = 0;
                ReadAppSettings(2, 7, 1, ref booteffect);
                byte[] sleep = new byte[3];
                ReadAppSettings(2, 37, 3, ref sleep[0]);
                int sleepsec = sleep[0] * 3600 + sleep[1] * 60 + sleep[2];
                byte sleepstatus = 0;
                ReadAppSettings(2, 36, 1, ref sleepstatus);

                byte displayBrightness = 255;
                if (rawBrightness <= 4)
                {
                    displayBrightness = (byte)Math.Min(255, (int)rawBrightness * 64);
                    if (displayBrightness == 0 && status != 0)
                    {
                        displayBrightness = 255;
                    }
                }
                else
                {
                    displayBrightness = rawBrightness;
                }

                object regBri = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbBrightness", null);
                if (regBri is int intBri)
                {
                    displayBrightness = (byte)Math.Max(0, Math.Min(255, intBri));
                }

                object regLight = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbLightState", null);
                if (regLight is int intLight)
                {
                    status = (byte)(intLight != 0 ? 1 : 0);
                }
                else
                {
                    // Default to enabled for first-time users
                    status = 1;
                    TurnOn();
                    SetBrightness(255);
                    displayBrightness = 255;
                }

                object regColor = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbColor", null);
                if (regColor is int intCol)
                {
                    colour[0] = (byte)((intCol >> 16) & 0xFF);
                    colour[1] = (byte)((intCol >> 8) & 0xFF);
                    colour[2] = (byte)(intCol & 0xFF);
                }
                else
                {
                    // Default to Blue (#FF0000FF) for new installs
                    colour[0] = 0;
                    colour[1] = 0;
                    colour[2] = 255;
                    SetColour(0, 0, 255);
                }

                object regUserSleep = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbUserSleepTimeout", null);
                if (regUserSleep is int userSleep && userSleep > 0)
                {
                    sleepsec = userSleep;
                    sleepstatus = 1;
                }
                else
                {
                    object regSleep = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbSleepTimer", null);
                    if (regSleep is int intSleep && intSleep > 0)
                    {
                        sleepsec = intSleep;
                        sleepstatus = 1;
                        try
                        {
                            Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbUserSleepTimeout", intSleep, Microsoft.Win32.RegistryValueKind.DWord);
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        // Default 30 seconds sleep timer for Clevo factory setting
                        sleepsec = 30;
                        sleepstatus = 1;
                        SetSleepTimer(30);
                    }
                }

                KeyboardEffect effect = null;
                object regEffectActive = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectActive", null);
                if (regEffectActive is int intActive)
                {
                    if (intActive != 0)
                    {
                        object regId = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectId", null);
                        object regUniq = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectUniqId", null);
                        object regName = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectName", null);
                        if (regId is int idVal)
                        {
                            byte uniqVal = regUniq is int u ? (byte)u : (byte)0;
                            string nameVal = regName as string;
                            effect = KeyboardEffect.LookUpEffect(idVal, uniqVal, nameVal);
                        }
                    }
                }
                else
                {
                    effect = KeyboardEffect.LookUpEffect(mode);
                }

                return new KeyboardStatus()
                {
                    BackLight = Color.FromArgb(displayBrightness, colour[0], colour[1], colour[2]),
                    Effect = effect,
                    LightState = status != 0,
                    BootEffect = booteffect != 0,
                    SleepSecond = sleepsec,
                    SleepState = sleepstatus != 0
                };
            }
            catch
            {
                return new KeyboardStatus()
                {
                    BackLight = Color.FromArgb(255, 0, 0, 255), // Default Blue (#FF0000FF)
                    Effect = null,
                    LightState = true,
                    BootEffect = false,
                    SleepSecond = 30,
                    SleepState = true
                };
            }
        }

        public static void SetBrightnessRealtime(byte value)
        {
            try
            {
                SetDCHU_Data(103, new byte[4] { value, 0, 0, 244 }, 4);
            }
            catch
            {
            }
        }

        public static void SetBrightness(byte value)
        {
            try
            {
                SetBrightnessRealtime(value);
                WriteAppSettings(2, 35, 1, ref value);
                try
                {
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbBrightness", (int)value, Microsoft.Win32.RegistryValueKind.DWord);
                }
                catch
                {
                }
            }
            catch
            {
            }
        }

        public static void SetColourRealtime(byte R, byte G, byte B)
        {
            try
            {
                SetDCHU_Data(103, new byte[4] { G, R, B, 240 }, 4);
                SetDCHU_Data(103, new byte[4] { G, R, B, 241 }, 4);
                SetDCHU_Data(103, new byte[4] { G, R, B, 242 }, 4);
                SetDCHU_Data(103, new byte[4] { G, R, B, 243 }, 4);
            }
            catch
            {
            }
        }

        public static void SetZoneColourRealtime(int zone, byte R, byte G, byte B)
        {
            if (zone < 0 || zone > 3) return;
            try
            {
                SetDCHU_Data(103, new byte[4] { G, R, B, (byte)(240 + zone) }, 4);
            }
            catch
            {
            }
        }

        public static void SetColour(byte R, byte G, byte B)
        {
            try
            {
                SetColourRealtime(R, G, B);
                byte[] colors = new byte[3] { R, G, B };
                WriteAppSettings(2, 81, 3, ref colors[0]);
                byte mode = 8;
                WriteAppSettings(2, 32, 1, ref mode);
                try
                {
                    int argb = unchecked((int)0xFF000000) | (R << 16) | (G << 8) | B;
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbColor", argb);
                }
                catch
                {
                }
            }
            catch
            {
            }
        }

        public static void SetColour(Color color)
        {
            SetColour(color.R, color.G, color.B);
        }

        public static void SetMode(KeyboardEffect mode)
        {
            if (mode == null) return;
            try
            {
                if (mode.IsIteHardwareEffect && IteHidController.IsDevicePresent)
                {
                    IteHidController.SetHardwareAnimation(mode.IteAnimMode, 0x05, 0x32, mode.IteBehaviour);
                }

                if (mode.UniqId != 0)
                {
                    SetDCHU_Data(103, new byte[4] { 0, 0, 0, mode.UniqId }, 4);
                    byte id = (byte)mode.Id;
                    WriteAppSettings(2, 32, 1, ref id);
                }
            }
            catch
            {
            }
        }

        public static void TurnOff()
        {
            try
            {
                if (IteHidController.IsDevicePresent)
                {
                    IteHidController.TurnOff();
                }

                SetBrightnessRealtime(0);
                SetDCHU_Data(103, new byte[4] { 7, 0, 0, 224 }, 4);
                SetDCHU_Data(103, new byte[4] { 0, 0, 0, 224 }, 4);
                byte val = 0;
                WriteAppSettings(2, 80, 1, ref val);
                WriteAppSettings(2, 84, 1, ref val);
                try
                {
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbLightState", 0, Microsoft.Win32.RegistryValueKind.DWord);
                }
                catch
                {
                }
            }
            catch
            {
            }
        }

        public static void TurnOn()
        {
            try
            {
                SetDCHU_Data(103, new byte[4] { 7, 0xB0, 7, 224 }, 4);
                SetDCHU_Data(103, new byte[4] { 0, 0, 1, 224 }, 4);
                byte val = 1;
                WriteAppSettings(2, 80, 1, ref val);
                WriteAppSettings(2, 84, 1, ref val);
                try
                {
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbLightState", 1, Microsoft.Win32.RegistryValueKind.DWord);
                }
                catch
                {
                }
            }
            catch
            {
            }
        }

        public static void OverrideBootEffect(bool value)
        {
            try
            {
                byte num = (byte)(value ? 1 : 0);
                SetDCHU_Data(121, new byte[4] { num, 0, 0, 30 }, 4);
                WriteAppSettings(2, 7, 1, ref num);
            }
            catch
            {
            }
        }

        public static void SetSleepTimer(int value)
        {
            try
            {
                byte active = 1;
                WriteAppSettings(2, 36, 1, ref active);
                byte[] times = new byte[3] { (byte)(value / 3600), (byte)(value / 60 % 60), (byte)(value % 60) };
                WriteAppSettings(2, 37, 3, ref times[0]);
                byte[] bytes = BitConverter.GetBytes(402653184 + (value << 8) | (int)byte.MaxValue);
                SetDCHU_Data(121, bytes, bytes.Length);
                try
                {
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbSleepTimer", value, Microsoft.Win32.RegistryValueKind.DWord);
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbUserSleepTimeout", value, Microsoft.Win32.RegistryValueKind.DWord);
                }
                catch
                {
                }
            }
            catch
            {
            }
        }

        public static void WriteAcpiSleepTimer(int value)
        {
            try
            {
                byte[] times = new byte[3] { (byte)(value / 3600), (byte)(value / 60 % 60), (byte)(value % 60) };
                WriteAppSettings(2, 37, 3, ref times[0]);
            }
            catch
            {
            }
        }

        public static void TurnSleepTimerOff(bool updateRegistry = true)
        {
            try
            {
                SetDCHU_Data(121, new byte[4] { 0, 0, 0, 24 }, 4);
                byte val = 0;
                WriteAppSettings(2, 36, 1, ref val);
                if (updateRegistry)
                {
                    try
                    {
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbSleepTimer", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbUserSleepTimeout", 0, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        public static void SetLightBarColor(byte r, byte g, byte b, byte brightness = 100)
        {
            try
            {
                IteHidController.SetLightBarColor(r, g, b, brightness);
            }
            catch
            {
            }
        }

        public static void SetLightBarOff()
        {
            try
            {
                IteHidController.SetLightBarOff();
            }
            catch
            {
            }
        }

        private static readonly byte[] BrightnessStepValues = new byte[] { 0, 64, 128, 192, 255 };

        public struct ColorPresetItem
        {
            public Color Color { get; set; }
            public string Name { get; set; }

            public ColorPresetItem(byte r, byte g, byte b, string name)
            {
                Color = Color.FromArgb(255, r, g, b);
                Name = name;
            }
        }

        public static readonly ColorPresetItem[] QuickColorPresets = new ColorPresetItem[]
        {
            new ColorPresetItem(255, 0, 0, "Red"),
            new ColorPresetItem(255, 128, 0, "Orange"),
            new ColorPresetItem(255, 255, 0, "Yellow"),
            new ColorPresetItem(0, 255, 0, "Green"),
            new ColorPresetItem(0, 255, 255, "Cyan"),
            new ColorPresetItem(0, 0, 255, "Blue"),
            new ColorPresetItem(255, 0, 255, "Purple"),
            new ColorPresetItem(255, 255, 255, "White")
        };

        private static int _currentColorPresetIndex = 0;
        private static readonly object _backlightAdjustLock = new object();
        private static DateTime _lastColorCycleTime = DateTime.MinValue;

        public static int BrightnessToStep(byte brightness, bool isLightOn)
        {
            if (!isLightOn || brightness == 0)
            {
                return 0;
            }
            if (brightness <= 64) return 1;
            if (brightness <= 128) return 2;
            if (brightness <= 192) return 3;
            return 4;
        }

        public static int CycleBrightness()
        {
            lock (_backlightAdjustLock)
            {
                try
                {
                    KeyboardStatus status = GetStatus();
                    int currentStep = BrightnessToStep(status.BackLight.A, status.LightState);
                    int nextStep = (currentStep + 1) % 5;
                    if (nextStep == 0)
                    {
                        TurnOff();
                    }
                    else
                    {
                        ApplyBrightnessStep(nextStep, status.BackLight);
                    }
                    NotifyKeyboardUiChanged();
                    return nextStep;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static int? IncreaseBrightness()
        {
            lock (_backlightAdjustLock)
            {
                try
                {
                    KeyboardStatus status = GetStatus();
                    int currentStep = BrightnessToStep(status.BackLight.A, status.LightState);
                    if (status.LightState && currentStep >= 4)
                    {
                        // Already at maximum Level 4 (100%): ignore key to avoid flicker
                        return null;
                    }
                    int nextStep = Math.Min(4, Math.Max(1, currentStep + 1));
                    ApplyBrightnessStep(nextStep, status.BackLight);
                    NotifyKeyboardUiChanged();
                    return nextStep;
                }
                catch
                {
                    return null;
                }
            }
        }

        public static int? DecreaseBrightness()
        {
            lock (_backlightAdjustLock)
            {
                try
                {
                    KeyboardStatus status = GetStatus();
                    int currentStep = BrightnessToStep(status.BackLight.A, status.LightState);
                    if (!status.LightState || currentStep <= 1)
                    {
                        // Already at minimum Level 1 (25%) or off: ignore key to avoid flicker
                        return null;
                    }
                    int nextStep = Math.Max(1, currentStep - 1);
                    ApplyBrightnessStep(nextStep, status.BackLight);
                    NotifyKeyboardUiChanged();
                    return nextStep;
                }
                catch
                {
                    return null;
                }
            }
        }

        private static void ApplyBrightnessStep(int step, Color currentColor)
        {
            int clamped = Math.Max(1, Math.Min(4, step));
            byte rawBrightness = BrightnessStepValues[clamped];
            TurnOn();
            KeyboardEffects.MaxBrightness = rawBrightness;
            SetBrightness(rawBrightness);
            if (!KeyboardEffect.IsAnySoftwareEffectRunning() && GetCapabilities().SupportsRgb)
            {
                SetColourRealtime(currentColor.R, currentColor.G, currentColor.B);
            }
        }

        public static bool ToggleBacklight(bool? forceState = null)
        {
            lock (_backlightAdjustLock)
            {
                try
                {
                    KeyboardStatus status = GetStatus();
                    bool targetOn = forceState ?? !status.LightState;
                    if (!targetOn)
                    {
                        TurnOff();
                    }
                    else
                    {
                        TurnOn();
                        int currentStep = BrightnessToStep(status.BackLight.A, true);
                        if (currentStep < 1) currentStep = 4;
                        ApplyBrightnessStep(currentStep, status.BackLight);
                    }
                    NotifyKeyboardUiChanged();
                    return targetOn;
                }
                catch
                {
                    return true;
                }
            }
        }

        public static ColorPresetItem? CycleColorPreset()
        {
            lock (_backlightAdjustLock)
            {
                DateTime now = DateTime.UtcNow;
                if ((now - _lastColorCycleTime).TotalMilliseconds < 350)
                {
                    return null;
                }
                _lastColorCycleTime = now;

                try
                {
                    if (!GetCapabilities().SupportsRgb)
                    {
                        return null;
                    }

                    KeyboardEffect.StopAllSoftwareEffects();

                    KeyboardStatus status = GetStatus();
                    int foundIndex = -1;
                    for (int i = 0; i < QuickColorPresets.Length; i++)
                    {
                        if (QuickColorPresets[i].Color.R == status.BackLight.R &&
                            QuickColorPresets[i].Color.G == status.BackLight.G &&
                            QuickColorPresets[i].Color.B == status.BackLight.B)
                        {
                            foundIndex = i;
                            break;
                        }
                    }

                    if (foundIndex >= 0)
                    {
                        _currentColorPresetIndex = (foundIndex + 1) % QuickColorPresets.Length;
                    }
                    else
                    {
                        _currentColorPresetIndex = (_currentColorPresetIndex + 1) % QuickColorPresets.Length;
                    }

                    ColorPresetItem nextPreset = QuickColorPresets[_currentColorPresetIndex];

                    byte targetBrightness = status.BackLight.A > 0 ? status.BackLight.A : (byte)255;
                    TurnOn();
                    SetBrightness(targetBrightness);
                    SetColour(nextPreset.Color.R, nextPreset.Color.G, nextPreset.Color.B);
                    NotifyKeyboardUiChanged();
                    return nextPreset;
                }
                catch
                {
                    return null;
                }
            }
        }

        private static void NotifyKeyboardUiChanged()
        {
            try
            {
                System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    var mainWin = System.Windows.Application.Current.MainWindow as MainWindow;
                    mainWin?.SyncKeyboardSettingsUI();
                }));
            }
            catch
            {
            }
        }
    }
}