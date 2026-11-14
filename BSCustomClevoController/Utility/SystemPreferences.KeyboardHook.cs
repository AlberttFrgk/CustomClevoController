using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BSCustomClevoController.Controls;

namespace BSCustomClevoController.Utility
{
    public static partial class SystemPreferences
    {
        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        private const int VK_VOLUME_MUTE = 0xAD;
        private const int VK_VOLUME_DOWN = 0xAE;
        private const int VK_VOLUME_UP = 0xAF;

        private const uint WM_SYSCOMMAND = 0x0112;
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
        private static readonly IntPtr SC_MONITORPOWER = new IntPtr(0xF170);
        private static readonly IntPtr MONITOR_OFF = new IntPtr(2);

        private static readonly NativeMethods.LowLevelKeyboardProc _hookCallback = HookProcedure;
        private static readonly HashSet<uint> _handledFnKeys = new HashSet<uint>();
        private static IntPtr _hookId = IntPtr.Zero;
        private static bool _isWinKeyLocked = false;
        private static bool _isSwapWinFn = false;
        private static bool _isFnLock = false;
        private static long _lastWinKeyOsdTicks;
        private static bool _hookWinDown = false;
        private static bool _hookAltDown = false;

        public static void UpdateHookState()
        {
            InstallKeyboardHook();
        }

        private static void InstallKeyboardHook()
        {
            if (_hookId != IntPtr.Zero) return;
            try
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    _hookId = NativeMethods.SetWindowsHookEx(
                        NativeMethods.WH_KEYBOARD_LL,
                        _hookCallback,
                        NativeMethods.GetModuleHandle(curModule.ModuleName),
                        0);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("SystemPreferences.InstallKeyboardHook", ex);
            }
        }

        private static void RemoveKeyboardHook()
        {
            if (_hookId != IntPtr.Zero)
            {
                try
                {
                    NativeMethods.UnhookWindowsHookEx(_hookId);
                }
                catch (Exception ex)
                {
                    AppLogger.Log("SystemPreferences.RemoveKeyboardHook", ex);
                }
                _hookId = IntPtr.Zero;
            }
        }

        private static IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var hookStruct = (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));

                // Ignore synthetic keys to prevent hook recursion and self-blocking
                const int LLKHF_INJECTED = 0x10;
                if ((hookStruct.flags & LLKHF_INJECTED) != 0)
                {
                    return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                int msg = (int)wParam;
                bool isKeyDown = (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN);
                bool isKeyUp = (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP);
                uint vk = hookStruct.vkCode;

                if (vk == NativeMethods.VK_LWIN || vk == NativeMethods.VK_RWIN)
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
                    || (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0
                    || (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0
                    || (NativeMethods.GetKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0
                    || (NativeMethods.GetKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0;
                bool altKeyActive = _hookAltDown
                    || (hookStruct.flags & 0x20) != 0
                    || (NativeMethods.GetAsyncKeyState(0x12) & 0x8000) != 0
                    || (NativeMethods.GetAsyncKeyState(0xA4) & 0x8000) != 0
                    || (NativeMethods.GetAsyncKeyState(0xA5) & 0x8000) != 0
                    || (NativeMethods.GetKeyState(0x12) & 0x8000) != 0;

                if (vk == 0x4B && ((winKeyActive && altKeyActive) || _handledFnKeys.Contains(0x4B)))
                {
                    if (isKeyDown)
                    {
                        _handledFnKeys.Add(0x4B);
                        NativeMethods.keybd_event(0xFF, 0, NativeMethods.KEYEVENTF_KEYUP, 0);
                        Task.Run(() => ToggleMicrophone());
                    }
                    else if (isKeyUp)
                    {
                        _handledFnKeys.Remove(0x4B);
                    }
                    return (IntPtr)1;
                }

                if (_isWinKeyLocked && (vk == NativeMethods.VK_LWIN || vk == NativeMethods.VK_RWIN))
                {
                    if (isKeyDown && OsdOverlay.IsEnabled && MonotonicClock.ElapsedMs(_lastWinKeyOsdTicks) >= 500.0)
                    {
                        _lastWinKeyOsdTicks = MonotonicClock.NowTicks();
                        OsdOverlay.ShowWinKeyLock(true);
                    }
                    return (IntPtr)1;
                }

                // Intercept Backlight keys when Win/Fn is active to prevent Windows Magnifier (Win + + / Win + -)
                if (winKeyActive)
                {
                    if (vk == 0xBB || vk == 0x6B)
                    {
                        if (isKeyDown)
                        {
                            int? step = KeyboardBackLight.IncreaseBrightness();
                            if (step.HasValue)
                            {
                                OsdOverlay.ShowBacklight(step.Value);
                            }
                        }
                        return (IntPtr)1;
                    }
                    if (vk == 0xBD || vk == 0x6D)
                    {
                        if (isKeyDown)
                        {
                            int? step = KeyboardBackLight.DecreaseBrightness();
                            if (step.HasValue)
                            {
                                OsdOverlay.ShowBacklight(step.Value);
                            }
                        }
                        return (IntPtr)1;
                    }
                    if (vk == 0x6A || vk == 0x38)
                    {
                        if (isKeyDown)
                        {
                            OsdOverlay.ShowBacklightToggle(KeyboardBackLight.ToggleBacklight());
                        }
                        return (IntPtr)1;
                    }
                    if (vk == 0xBF || vk == 0x6F || vk == 0x20)
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

                if (vk == NativeMethods.VK_F24)
                {
                    _lastHardwareTouchpadKeyTicks = MonotonicClock.NowTicks();
                }

                if (OsdOverlay.IsEnabled && isKeyUp)
                {
                    if (vk == NativeMethods.VK_CAPITAL)
                    {
                        OsdOverlay.ShowCaps(GetCapsLockState());
                    }
                    else if (vk == NativeMethods.VK_NUMLOCK)
                    {
                        OsdOverlay.ShowNum(GetNumLockState());
                    }
                    else if (vk == NativeMethods.VK_SCROLL)
                    {
                        OsdOverlay.ShowScroll(GetScrollLockState());
                    }
                    else if (vk == VK_AIRPLANE || vk == 0xF2)
                    {
                        Task.Delay(120).ContinueWith(_ =>
                        {
                            bool airState = GetAirplaneModeState();
                            _lastAirplaneState = airState;
                            _suppressAirplaneOsdUntilTicks = MonotonicClock.DeadlineFromNowMs(2000.0);
                            OsdOverlay.ShowAirplane(airState);
                        });
                    }
                    else if (vk == NativeMethods.VK_F24)
                    {
                        Task.Delay(120).ContinueWith(_ =>
                        {
                            bool state = Touchpad.SyncFromPrecisionTouchpad();
                            OsdOverlay.ShowTouchpad(state);
                        });
                    }
                }

                if (_isFnLock && _lastEcKeyboardState[0] == 0 && IsFnKey(vk))
                {
                    // Allow OS shortcuts (Alt+F4, Ctrl+F5, Shift+F10) through normally
                    bool ctrlDown = (NativeMethods.GetKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
                    bool shiftDown = (NativeMethods.GetKeyState(0x10) & 0x8000) != 0;
                    bool altDown = (NativeMethods.GetKeyState(0x12) & 0x8000) != 0;
                    if (ctrlDown || altDown || shiftDown)
                    {
                        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                    }

                    if (isKeyDown)
                    {
                        if (HandleFnAction(vk))
                        {
                            _handledFnKeys.Add(vk);
                            return (IntPtr)1;
                        }
                    }
                    else if (isKeyUp && _handledFnKeys.Remove(vk))
                    {
                        return (IntPtr)1;
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static bool IsFnKey(uint vk) => vk >= 0x70 && vk <= 0x7B;

        private static bool HandleFnAction(uint vk)
        {
            switch (vk)
            {
                case 0x70:
                    OsdOverlay.ShowTouchpad(Touchpad.ToggleTouchpad());
                    return true;
                case 0x71:
                    NativeMethods.PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, MONITOR_OFF);
                    return true;
                case 0x72:
                    NativeMethods.keybd_event(VK_VOLUME_MUTE, 0, 0, 0);
                    NativeMethods.keybd_event(VK_VOLUME_MUTE, 0, NativeMethods.KEYEVENTF_KEYUP, 0);
                    return true;
                case 0x73:
                    CycleKeyboardBacklight();
                    return true;
                case 0x74:
                    NativeMethods.keybd_event(VK_VOLUME_DOWN, 0, 0, 0);
                    NativeMethods.keybd_event(VK_VOLUME_DOWN, 0, NativeMethods.KEYEVENTF_KEYUP, 0);
                    return true;
                case 0x75:
                    NativeMethods.keybd_event(VK_VOLUME_UP, 0, 0, 0);
                    NativeMethods.keybd_event(VK_VOLUME_UP, 0, NativeMethods.KEYEVENTF_KEYUP, 0);
                    return true;
                case 0x76:
                    NativeMethods.keybd_event(NativeMethods.VK_LWIN, 0, 0, 0);
                    NativeMethods.keybd_event(0x50, 0, 0, 0);
                    NativeMethods.keybd_event(0x50, 0, NativeMethods.KEYEVENTF_KEYUP, 0);
                    NativeMethods.keybd_event(NativeMethods.VK_LWIN, 0, NativeMethods.KEYEVENTF_KEYUP, 0);
                    return true;
                case 0x77:
                    AdjustScreenBrightness(-10);
                    return true;
                case 0x78:
                    AdjustScreenBrightness(10);
                    return true;
                case 0x79:
                    ToggleCamera();
                    return true;
                case 0x7A:
                    ToggleAirplaneMode();
                    return true;
                case 0x7B:
                    SetSuspendState(false, false, false);
                    return true;
            }
            return false;
        }
    }
}

