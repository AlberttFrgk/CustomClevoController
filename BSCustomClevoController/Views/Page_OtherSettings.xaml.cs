using BSCustomClevoController.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BSCustomClevoController.Controls;

namespace BSCustomClevoController.Views
{
    public partial class Page_OtherSettings : Page
    {
        private readonly DispatcherTimer _prefSyncTimer;
        private int _nativeMaxRefreshRate = 60;
        private DateTime _syncPausedUntilUtc = DateTime.MinValue;

        public Page_OtherSettings()
        {
            InitializeComponent();
            InitializePerformanceTuning();
            InitializePlatformTools();
            UpdateDisplayRefreshRateUI();
            SyncHardwarePreferences(true);

            _prefSyncTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(750)
            };
            _prefSyncTimer.Tick += (s, e) =>
            {
                if (DateTime.UtcNow >= _syncPausedUntilUtc)
                {
                    SyncHardwarePreferences();
                }
            };
            _prefSyncTimer.Start();
        }

        private void InitializePlatformTools()
        {
            // 1. GPU MUX Switch (Visible on all Clevo laptops; disabled if no hardware multiplexer)
            bool muxSupported = Capabilities.IsSupported("SupportMSHybrid_dGPUSwicth");
            GpuMuxSwitch.IsEnabled = muxSupported;
            GpuMuxCard.Opacity = muxSupported ? 1.0 : 0.55;
            if (muxSupported)
            {
                GpuMuxBadgeText.Text = "Hardware MUX Ready";
                GpuMuxBadgeText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                GpuMuxBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                GpuMuxBadge.Background = new SolidColorBrush(Color.FromRgb(27, 62, 50));
            }
            else
            {
                GpuMuxBadgeText.Text = "Not supported on this model";
                GpuMuxBadgeText.Foreground = (Brush)FindResource("Amoled.TextMuted");
                GpuMuxBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 70));
                GpuMuxBadge.Background = new SolidColorBrush(Color.FromRgb(28, 28, 36));
            }

            // 2. Wake on LAN
            bool wolSupported = Capabilities.IsSupported("WakeUpLanSupport");
            PrefWolSwitch.IsEnabled = wolSupported;
            PrefWolCard.Opacity = wolSupported ? 1.0 : 0.55;
            PrefWolBadge.Visibility = wolSupported ? Visibility.Collapsed : Visibility.Visible;

            // 3. Ethernet LED
            bool ethLedSupported = Capabilities.IsSupported("SupportEthernetLED");
            PrefEthLedSwitch.IsEnabled = ethLedSupported;
            PrefEthLedCard.Opacity = ethLedSupported ? 1.0 : 0.55;
            PrefEthLedBadge.Visibility = ethLedSupported ? Visibility.Collapsed : Visibility.Visible;

            // 4. USB Hub Reset
            bool usbResetSupported = Capabilities.IsSupported("SupportUsbHubReset");
            PrefUsbResetBtn.IsEnabled = usbResetSupported;
            PrefUsbResetCard.Opacity = usbResetSupported ? 1.0 : 0.55;
            PrefUsbResetBadge.Visibility = usbResetSupported ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateDisplayRefreshRateUI()
        {
            try
            {
                int currentHz = DisplayControl.GetCurrentRefreshRate();
                List<int> availableRates = DisplayControl.GetAvailableRefreshRates();

                _nativeMaxRefreshRate = availableRates.Count > 0 ? availableRates[availableRates.Count - 1] : currentHz;
                RefreshRateCurrentText.Text = $"{currentHz} Hz";

                RefreshNativeBtn.Content = $"{_nativeMaxRefreshRate} Hz (Native)";

                if (availableRates.Count <= 1 || _nativeMaxRefreshRate == 60)
                {
                    Refresh60Btn.IsEnabled = false;
                    RefreshNativeBtn.IsEnabled = false;
                    RefreshRateBadge.ToolTip = "Fixed display panel refresh rate detected.";
                }
                else
                {
                    Refresh60Btn.IsEnabled = currentHz != 60;
                    RefreshNativeBtn.IsEnabled = currentHz != _nativeMaxRefreshRate;
                }
            }
            catch
            {
            }
        }

        private void InitializePerformanceTuning()
        {
            try
            {
                // 1. CPU Telemetry, Power Target, and Status
                if (CpuNameText != null)
                {
                    CpuNameText.Text = CpuPowerControl.CpuModel;
                }

                bool cpuOcSupported = CpuPowerControl.IsSupported;
                if (CpuPowerSlider != null)
                {
                    CpuPowerSlider.IsEnabled = cpuOcSupported;
                }

                if (cpuOcSupported)
                {
                    if (CpuPerformanceCard != null)
                    {
                        CpuPerformanceCard.Opacity = 1.0;
                    }
                    if (CpuOcBadge != null)
                    {
                        CpuOcBadge.Background = new SolidColorBrush(Color.FromRgb(27, 62, 50));
                        CpuOcBadge.BorderBrush = (Brush)FindResource("Amoled.Success");
                    }
                    if (CpuOcBadgeText != null)
                    {
                        CpuOcBadgeText.Foreground = (Brush)FindResource("Amoled.Success");
                    }
                    if (CpuOcDescriptionText != null)
                    {
                        CpuOcDescriptionText.Text = "Adjusts processor power limits for sustained boost performance";
                    }
                    if (CpuOcStatusSubText != null)
                    {
                        CpuOcStatusSubText.Text = "Range: 0W to +25W";
                    }

                    int initialCpuPower = CpuPowerControl.GetPowerOffset();
                    if (CpuPowerSlider != null)
                    {
                        CpuPowerSlider.Value = initialCpuPower;
                    }
                    UpdateCpuLabel(initialCpuPower);
                }
                else
                {
                    if (CpuPerformanceCard != null)
                    {
                        CpuPerformanceCard.Opacity = 0.65;
                    }
                    if (CpuOcBadge != null)
                    {
                        CpuOcBadge.Background = new SolidColorBrush(Color.FromArgb(40, 100, 100, 110));
                        CpuOcBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 140, 140, 150));
                    }
                    if (CpuOcBadgeText != null)
                    {
                        CpuOcBadgeText.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 170));
                        CpuOcBadgeText.Text = "Locked (OEM)";
                    }
                    if (CpuOcDescriptionText != null)
                    {
                        CpuOcDescriptionText.Text = "Processor power and speed managed by hardware";
                    }
                    if (CpuOcStatusSubText != null)
                    {
                        CpuOcStatusSubText.Text = "Locked by CPU firmware";
                    }
                    if (CpuPowerSlider != null)
                    {
                        CpuPowerSlider.Value = 0;
                    }
                }

                // 2. GPU Power Target & Overclock
                if (NvidiaPowerControl.IsNvidiaPresent)
                {
                    NvidiaTgpCard.Visibility = Visibility.Visible;
                    NvidiaGpuNameText.Text = NvidiaPowerControl.GpuModel;
                    int initialTgp = NvidiaPowerControl.GetTgpOffset();
                    TgpOffsetSlider.Value = initialTgp;
                    UpdateTgpLabel(initialTgp);
                }
                else
                {
                    NvidiaTgpCard.Visibility = Visibility.Collapsed;
                }

                // 3. Model-Specific Overclocking Features
                bool cpuMultiplierSupported = Capabilities.IsSupported("SupportCPUOC");
                if (CpuMultiplierOcSwitch != null) CpuMultiplierOcSwitch.IsEnabled = cpuMultiplierSupported;
                if (CpuMultiplierOcCard != null) CpuMultiplierOcCard.Opacity = cpuMultiplierSupported ? 1.0 : 0.55;
                if (CpuMultiplierOcBadge != null)
                {
                    CpuMultiplierOcBadge.Visibility = cpuMultiplierSupported ? Visibility.Collapsed : Visibility.Visible;
                }

                bool allModeGpuSupported = Capabilities.IsSupported("SupportAllModeGPUOC");
                if (AllModeGpuOcSwitch != null) AllModeGpuOcSwitch.IsEnabled = allModeGpuSupported;
                if (AllModeGpuOcCard != null) AllModeGpuOcCard.Opacity = allModeGpuSupported ? 1.0 : 0.55;
                if (AllModeGpuOcBadge != null)
                {
                    AllModeGpuOcBadge.Visibility = allModeGpuSupported ? Visibility.Collapsed : Visibility.Visible;
                }

                bool xmpSupported = Capabilities.IsSupported("SupportXMP");
                if (XmpSwitch != null) XmpSwitch.IsEnabled = xmpSupported;
                if (XmpCard != null) XmpCard.Opacity = xmpSupported ? 1.0 : 0.55;
                if (XmpBadge != null)
                {
                    XmpBadge.Visibility = xmpSupported ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            catch
            {
            }
        }

        private void SyncHardwarePreferences(bool queryEc = false)
        {
            try
            {
                if (queryEc)
                {
                    SystemPreferences.QueryKeyboardSettingsFromEC();
                }

                // 1. Win Key Lock
                if (PrefWinKeySwitch != null)
                {
                    PrefWinKeySwitch.IsChecked = SystemPreferences.IsWinKeyLocked;
                }

                // 2. Swap Win-Fn
                if (PrefSwapWinFnSwitch != null)
                {
                    if (!SystemPreferences.IsSwapWinFnSupported)
                    {
                        PrefSwapWinFnSwitch.IsEnabled = false;
                        PrefSwapWinFnSwitch.Opacity = 0.35;
                        PrefSwapWinFnSwitch.IsChecked = false;
                        if (PrefSwapWinFnSubtitle != null)
                        {
                            PrefSwapWinFnSubtitle.Text = "Not supported on this model";
                        }
                    }
                    else
                    {
                        PrefSwapWinFnSwitch.IsEnabled = true;
                        PrefSwapWinFnSwitch.Opacity = 1.0;
                        PrefSwapWinFnSwitch.IsChecked = SystemPreferences.IsSwapWinFn;
                    }
                }

                // 3. Fn-Key Lock
                if (PrefFnLockSwitch != null)
                {
                    PrefFnLockSwitch.IsChecked = SystemPreferences.IsFnLock;
                }

                // 4. Touchpad
                if (PrefTouchpadSwitch != null)
                {
                    PrefTouchpadSwitch.IsChecked = Touchpad.GetTouchpadState();
                }

                // 5. Camera
                if (PrefCameraSwitch != null)
                {
                    PrefCameraSwitch.IsChecked = SystemPreferences.GetCameraState();
                }

                if (PrefMicSwitch != null)
                {
                    PrefMicSwitch.IsChecked = SystemPreferences.GetMicrophoneState();
                }

                // 6. Airplane Mode
                if (PrefAirplaneSwitch != null)
                {
                    PrefAirplaneSwitch.IsChecked = SystemPreferences.GetAirplaneModeState();
                }

                // 7. Caps Lock
                if (PrefCapsSwitch != null)
                {
                    PrefCapsSwitch.IsChecked = SystemPreferences.GetCapsLockState();
                }

                // 8. Num Lock
                if (PrefNumSwitch != null)
                {
                    PrefNumSwitch.IsChecked = SystemPreferences.GetNumLockState();
                }

                // 9. Scroll Lock
                if (PrefScrSwitch != null)
                {
                    PrefScrSwitch.IsChecked = SystemPreferences.GetScrollLockState();
                }

                // 10. GPU Tuning & Overclock
                if (PerformanceTuningSection != null && NvidiaTgpCard != null && NvidiaTgpCard.Visibility == Visibility.Visible && NvidiaPowerControl.IsNvidiaPresent)
                {
                    bool isPowerSaver = (PowerMode.GetStatus() == PowerMode.Mode.Powersaving);
                    if (isPowerSaver)
                    {
                        TgpOffsetSlider.IsEnabled = false;
                        TgpOffsetText.Text = "Eco (Disabled)";
                    }
                    else
                    {
                        TgpOffsetSlider.IsEnabled = true;
                        int currentOffset = (int)TgpOffsetSlider.Value;
                        UpdateTgpLabel(currentOffset);
                    }
                }
            }
            catch
            {
            }
        }

        private void TurnOffScreenBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemPreferences.TurnOffScreen();
        }

        private void Refresh60Btn_Click(object sender, RoutedEventArgs e)
        {
            DisplayControl.SetRefreshRate(60);
            UpdateDisplayRefreshRateUI();
        }

        private void RefreshNativeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_nativeMaxRefreshRate > 0)
            {
                DisplayControl.SetRefreshRate(_nativeMaxRefreshRate);
                UpdateDisplayRefreshRateUI();
            }
        }

        private void GpuMuxSwitch_Click(object sender, RoutedEventArgs e)
        {
            bool discreteOnly = GpuMuxSwitch.IsChecked == true;
            Capabilities.SetGpuMuxMode(discreteOnly);
            MessageBox.Show(
                discreteOnly
                    ? "Graphics switched to Dedicated GPU. Please restart your laptop to apply the change."
                    : "Graphics switched to Hybrid mode (power saving). Please restart your laptop to apply the change.",
                "GPU MUX Switch",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void PrefWolBtn_Click(object sender, RoutedEventArgs e)
        {
            Capabilities.SetWakeOnLan(PrefWolSwitch.IsChecked == true);
        }

        private void PrefEthLedBtn_Click(object sender, RoutedEventArgs e)
        {
            Capabilities.SetEthernetLed(PrefEthLedSwitch.IsChecked == true);
        }

        private void PrefUsbResetBtn_Click(object sender, RoutedEventArgs e)
        {
            Capabilities.ResetUsbHub();
            MessageBox.Show("The internal USB hub has been reset.", "USB Hub Reset", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetKeyboardPrefsBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemPreferences.ResetKeyboardPreferences();
            SyncHardwarePreferences();
            MessageBox.Show(
                "Keyboard hardware settings (Fn Lock, Win Key Lock, Swap Win-Fn) have been reset to factory defaults.",
                "Keyboard Reset",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void PrefWinKeyBtn_Click(object sender, RoutedEventArgs e)
        {
            _syncPausedUntilUtc = DateTime.UtcNow.AddMilliseconds(2500);
            if (PrefWinKeySwitch != null)
            {
                SystemPreferences.SetWinKeyLock(PrefWinKeySwitch.IsChecked == true);
            }
            else
            {
                SystemPreferences.ToggleWinKeyLock();
            }
            SyncHardwarePreferences();
        }

        private void PrefSwapWinFnBtn_Click(object sender, RoutedEventArgs e)
        {
            _syncPausedUntilUtc = DateTime.UtcNow.AddMilliseconds(2500);
            if (PrefSwapWinFnSwitch != null)
            {
                SystemPreferences.SetSwapWinFn(PrefSwapWinFnSwitch.IsChecked == true);
            }
            else
            {
                SystemPreferences.ToggleSwapWinFn();
            }
            SyncHardwarePreferences();
        }

        private void PrefFnLockBtn_Click(object sender, RoutedEventArgs e)
        {
            _syncPausedUntilUtc = DateTime.UtcNow.AddMilliseconds(2500);
            if (PrefFnLockSwitch != null)
            {
                SystemPreferences.SetFnLock(PrefFnLockSwitch.IsChecked == true);
            }
            else
            {
                SystemPreferences.ToggleFnLock();
            }
            SyncHardwarePreferences();
        }

        private void PrefTouchpadBtn_Click(object sender, RoutedEventArgs e)
        {
            _syncPausedUntilUtc = DateTime.UtcNow.AddMilliseconds(2500);
            bool targetState = (PrefTouchpadSwitch != null && PrefTouchpadSwitch.IsChecked.HasValue)
                ? PrefTouchpadSwitch.IsChecked.Value
                : !Touchpad.GetTouchpadState();
            Touchpad.SetTouchpadState(targetState);
            if (PrefTouchpadSwitch != null)
            {
                PrefTouchpadSwitch.IsChecked = targetState;
            }
        }

        private async void PrefMicBtn_Click(object sender, RoutedEventArgs e)
        {
            bool targetState = (PrefMicSwitch != null && PrefMicSwitch.IsChecked.HasValue)
                ? PrefMicSwitch.IsChecked.Value
                : !SystemPreferences.GetMicrophoneState();
            SystemPreferences.SetMicrophoneState(targetState);
            OsdOverlay.ShowMicrophone(targetState);
            if (PrefMicSwitch != null)
            {
                PrefMicSwitch.IsChecked = targetState;
            }
            await Task.Delay(200);
            SyncHardwarePreferences();
        }

        private async void PrefCameraBtn_Click(object sender, RoutedEventArgs e)
        {
            bool newState = SystemPreferences.ToggleCamera();
            if (PrefCameraSwitch != null)
            {
                PrefCameraSwitch.IsChecked = newState;
            }
            await Task.Delay(300);
            SyncHardwarePreferences();
        }

        private async void PrefAirplaneBtn_Click(object sender, RoutedEventArgs e)
        {
            bool newState = SystemPreferences.ToggleAirplaneMode();
            if (PrefAirplaneSwitch != null)
            {
                PrefAirplaneSwitch.IsChecked = newState;
            }
            await Task.Delay(300);
            SyncHardwarePreferences();
        }

        private void PrefCapsLockBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemPreferences.ToggleCapsLock();
            SyncHardwarePreferences();
        }

        private void PrefNumLockBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemPreferences.ToggleNumLock();
            SyncHardwarePreferences();
        }

        private void PrefScrLockBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemPreferences.ToggleScrollLock();
            SyncHardwarePreferences();
        }

        private void CpuPowerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CpuPowerSlider == null || CpuOcBadgeText == null || !CpuPowerControl.IsSupported) return;

            int offset = (int)CpuPowerSlider.Value;
            UpdateCpuLabel(offset);
            CpuPowerControl.SetPowerOffset(offset);
        }

        private void UpdateCpuLabel(int offset)
        {
            if (CpuOcBadgeText == null) return;

            if (offset == 0)
            {
                CpuOcBadgeText.Text = "+0W (Stock)";
            }
            else if (offset >= 25)
            {
                CpuOcBadgeText.Text = "+" + offset.ToString() + "W (Max Boost)";
            }
            else
            {
                CpuOcBadgeText.Text = "+" + offset.ToString() + "W";
            }
        }

        private void TgpOffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TgpOffsetSlider == null || TgpOffsetText == null) return;

            int offset = (int)TgpOffsetSlider.Value;
            UpdateTgpLabel(offset);
            NvidiaPowerControl.SetTgpOffset(offset);
        }

        private void UpdateTgpLabel(int offset)
        {
            if (offset == 0)
            {
                TgpOffsetText.Text = "+0W (Stock)";
            }
            else if (offset >= 25)
            {
                TgpOffsetText.Text = "+" + offset.ToString() + "W (Max Boost)";
            }
            else
            {
                TgpOffsetText.Text = "+" + offset.ToString() + "W";
            }
        }
    }
}
