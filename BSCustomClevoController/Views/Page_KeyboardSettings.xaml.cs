using BSCustomClevoController.Utility;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static BSCustomClevoController.Utility.Structs;
using BSCustomClevoController.Controls;

namespace BSCustomClevoController.Views
{
    public partial class Page_KeyboardSettings : Page
    {
        private readonly DispatcherTimer _colorThrottleTimer;
        private readonly DispatcherTimer _saveSettingsTimer;
        private Color? _pendingColor;
        private byte _pendingBrightness = 255;
        private bool _isInitializing = true;
        private bool _isUpdatingEffects = false;
        private bool _hasLoadedOnce = false;
        private bool? _lastAcStateInUI = null;
        private readonly KeyboardHardwareCapabilities _caps;

        public Page_KeyboardSettings()
        {
            InitializeComponent();

            _colorThrottleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(35)
            };
            _colorThrottleTimer.Tick += ColorThrottleTimer_Tick;

            _saveSettingsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(600)
            };
            _saveSettingsTimer.Tick += SaveSettingsTimer_Tick;

            KeyboardStatus status = KeyboardBackLight.GetStatus();
            _caps = KeyboardBackLight.GetCapabilities();

            // Color controls: keep visible; disable if hardware is monochrome
            if (!_caps.SupportsRgb)
            {
                QuickColorPresetsCard.IsEnabled = false;
                ColorCanvasCard.IsEnabled = false;
                QuickColorPresetsCard.Opacity = 0.5;
                ColorCanvasCard.Opacity = 0.5;
                MonochromeColorBadge.Visibility = Visibility.Visible;
            }
            else
            {
                QuickColorPresetsCard.IsEnabled = true;
                ColorCanvasCard.IsEnabled = true;
                MonochromeColorBadge.Visibility = Visibility.Collapsed;
            }

            // Chassis Light Bar (keep visible; disable if unsupported)
            bool lightBarSupported = _caps.SupportsLightBar || IteHidController.IsLightBarPresent || Capabilities.IsSupported("SupportLightbar");
            LightBarCheckBox.IsEnabled = lightBarSupported;
            LightBarRow.Opacity = lightBarSupported ? 1.0 : 0.55;
            LightBarBadge.Visibility = lightBarSupported ? Visibility.Collapsed : Visibility.Visible;
            if (lightBarSupported)
            {
                LightBarCheckBox.IsChecked = status.LightState;
            }

            // Lid Logo Lighting (keep visible; disable if unsupported)
            bool logoSupported = Capabilities.IsSupported("SupportLogoColor");
            LogoCheckBox.IsEnabled = logoSupported;
            LogoRow.Opacity = logoSupported ? 1.0 : 0.55;
            LogoBadge.Visibility = logoSupported ? Visibility.Collapsed : Visibility.Visible;

            // Touchpad Perimeter Lighting (keep visible; disable if unsupported)
            bool tpLedSupported = Capabilities.IsSupported("SupportTpColor");
            TouchpadLedCheckBox.IsEnabled = tpLedSupported;
            TouchpadLedRow.Opacity = tpLedSupported ? 1.0 : 0.55;
            TouchpadLedBadge.Visibility = tpLedSupported ? Visibility.Collapsed : Visibility.Visible;

            // Override Boot Effect (keep visible; disable if unsupported)
            bool bootOverrideSupported = _caps.SupportsBootEffectOverride;
            BootEffectCheckBox.IsEnabled = bootOverrideSupported;
            BootEffectRow.Opacity = bootOverrideSupported ? 1.0 : 0.55;
            BootEffectBadge.Visibility = bootOverrideSupported ? Visibility.Collapsed : Visibility.Visible;
            if (bootOverrideSupported)
            {
                BootEffectCheckBox.IsChecked = status.BootEffect;
            }

            object regSpeed = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbAnimationSpeed", null);
            int savedSpeed = (regSpeed is int sp && sp >= 1 && sp <= 3) ? sp : 2;
            KeyboardEffect.CurrentAnimationSpeed = savedSpeed;
            UpdateSpeedButtonsUI(savedSpeed);

            // Initialize software animations toggle from registry (default: disabled)
            bool isBattery = KeyboardEffects.IsRunningOnBattery;
            object regSoftware = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "SoftwareEffects", null);
            bool allowSoftware = regSoftware is int intSoft && intSoft == 1;
            if (status.Effect != null && status.Effect.IsSoftware && !isBattery)
            {
                allowSoftware = true;
                try
                {
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "SoftwareEffects", 1, Microsoft.Win32.RegistryValueKind.DWord);
                }
                catch
                {
                }
            }
            SoftwareEffectsSwitch.IsChecked = allowSoftware && !isBattery;
            SoftwareEffectsSwitch.IsEnabled = !isBattery;
            if (SoftwareBatteryBadge != null)
            {
                SoftwareBatteryBadge.Visibility = isBattery ? Visibility.Visible : Visibility.Collapsed;
            }

            // Populate effects list with hardware and software effects
            UpdateAvailableEffects();

            // Initialize lighting master status
            LedStatusCheckBox.IsChecked = status.LightState;

            // Initialize brightness slider (preserve saved brightness even if backlight is currently off)
            int initialStep = KeyboardBackLight.BrightnessToStep(status.BackLight.A, true);
            if (initialStep < 1) initialStep = 4;
            _pendingBrightness = (byte)(initialStep == 1 ? 64 : (initialStep == 2 ? 128 : (initialStep == 3 ? 192 : 255)));
            KeyboardEffects.MaxBrightness = _pendingBrightness;
            KeyboardBrightnessSlider.Value = initialStep;
            BrightnessPercentText.Text = $"{initialStep * 25}%";

            // Initialize color canvas
            LedColorCanvas.SelectedColor = Color.FromRgb(status.BackLight.R, status.BackLight.G, status.BackLight.B);

            // Initialize effect selection
            if (status.Effect != null && EffectsListBox.ItemsSource is KeyboardEffect[] available)
            {
                bool matched = false;
                for (int i = 0; i < available.Length; i++)
                {
                    if (available[i].UniqId == status.Effect.UniqId && available[i].Id == status.Effect.Id)
                    {
                        EffectsListBox.SelectedIndex = i;
                        matched = true;
                        break;
                    }
                }
                ModeStatus.IsChecked = matched;
            }

            // Initialize boot effect override
            BootEffectCheckBox.IsChecked = status.BootEffect;

            // Initialize sleep timer
            int effectiveSleep = status.SleepSecond;
            if (effectiveSleep <= 0)
            {
                object regUserSleep = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbUserSleepTimeout", null);
                if (regUserSleep is int us && us > 0)
                {
                    effectiveSleep = us;
                }
                else
                {
                    object regSleep = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbSleepTimer", null);
                    if (regSleep is int s && s > 0)
                    {
                        effectiveSleep = s;
                    }
                    else
                    {
                        effectiveSleep = 30; // Clevo factory default (30 seconds)
                    }
                }
            }

            KeyboarSleepTimerHours.Text = Convert.ToString(effectiveSleep / 3600);
            KeyboarSleepTimerMunites.Text = Convert.ToString((effectiveSleep % 3600) / 60);
            KeyboarSleepTimerSeconds.Text = Convert.ToString(effectiveSleep % 60);

            KeyboarSleepTimerHours.TextChanged += KeyboarSleepTimer_TextChanged;
            KeyboarSleepTimerMunites.TextChanged += KeyboarSleepTimer_TextChanged;
            KeyboarSleepTimerSeconds.TextChanged += KeyboarSleepTimer_TextChanged;

            KeyboarSleepTimerHours.KeyDown += KeyboarSleepTimer_KeyDown;
            KeyboarSleepTimerMunites.KeyDown += KeyboarSleepTimer_KeyDown;
            KeyboarSleepTimerSeconds.KeyDown += KeyboarSleepTimer_KeyDown;
            KeyboarSleepTimerHours.LostFocus += KeyboarSleepTimer_LostFocus;
            KeyboarSleepTimerMunites.LostFocus += KeyboarSleepTimer_LostFocus;
            KeyboarSleepTimerSeconds.LostFocus += KeyboarSleepTimer_LostFocus;

            _isInitializing = false;

            if (ModeStatus.IsChecked == true && (status.Effect == null || status.Effect.IsSoftware))
            {
                SetSleepTimerPaused(true);
            }
            UpdateColorControlsState();
            UpdateBootEffectControlState();

            Loaded += (s, e) =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    HideAlphaChannelFromCanvas(LedColorCanvas);
                    StyleColorCanvasElements(LedColorCanvas);
                }));

                bool isAc = System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;
                if (!_hasLoadedOnce)
                {
                    _hasLoadedOnce = true;
                    _lastAcStateInUI = isAc;
                    SyncPowerBadgeUI(isAc);
                }
                else if (!_lastAcStateInUI.HasValue || _lastAcStateInUI.Value != isAc)
                {
                    _lastAcStateInUI = isAc;
                    UpdateBatterySoftwareAnimationState(isAc);
                }
            };

            LedColorCanvas.Loaded += (s, e) =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    HideAlphaChannelFromCanvas(LedColorCanvas);
                    StyleColorCanvasElements(LedColorCanvas);
                }));
            };
        }

        private void ColorThrottleTimer_Tick(object sender, EventArgs e)
        {
            _colorThrottleTimer.Stop();
            if (_pendingColor.HasValue)
            {
                Color c = _pendingColor.Value;
                KeyboardBackLight.SetColourRealtime(c.R, c.G, c.B);
                KeyboardBackLight.SetBrightnessRealtime(_pendingBrightness);

                if (LightBarCheckBox?.IsChecked == true)
                {
                    byte lbBri = (byte)Math.Round(_pendingBrightness / 255.0 * 100.0);
                    KeyboardBackLight.SetLightBarColor(c.R, c.G, c.B, lbBri);
                }

                // Restart debounce timer to save to ACPI flash when user finishes adjusting
                _saveSettingsTimer.Stop();
                _saveSettingsTimer.Start();
            }
        }

        private void SaveSettingsTimer_Tick(object sender, EventArgs e)
        {
            _saveSettingsTimer.Stop();
            if (_pendingColor.HasValue)
            {
                Color c = _pendingColor.Value;
                _pendingColor = null;
                KeyboardBackLight.SetColour(c.R, c.G, c.B);
            }
            KeyboardBackLight.SetBrightness(_pendingBrightness);
        }

        private void LedColorCanvas_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (_isInitializing) return;

            if (LedColorCanvas.SelectedColor.HasValue)
            {
                Color chosen = LedColorCanvas.SelectedColor.Value;
                _pendingColor = Color.FromRgb(chosen.R, chosen.G, chosen.B);
                LedColorCanvas.A = 255;

                // If user changes color, turn off animated effects to show static color
                if (ModeStatus.IsChecked == true)
                {
                    ModeStatus.IsChecked = false;
                }

                if (LedStatusCheckBox.IsChecked != true)
                {
                    LedStatusCheckBox.IsChecked = true;
                }

                if (!_colorThrottleTimer.IsEnabled)
                {
                    _colorThrottleTimer.Start();
                }
            }
        }

        private void KeyboardBrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BrightnessPercentText == null) return;

            int step = Math.Max(1, Math.Min(4, (int)Math.Round(e.NewValue)));
            byte val = (byte)(step == 1 ? 64 : (step == 2 ? 128 : (step == 3 ? 192 : 255)));
            _pendingBrightness = val;
            KeyboardEffects.MaxBrightness = val;
            BrightnessPercentText.Text = $"{step * 25}%";

            if (_isInitializing) return;

            bool isModeActive = ModeStatus.IsChecked == true;
            KeyboardEffect effect = EffectsListBox.SelectedItem as KeyboardEffect;
            bool isSoftwareBreathRunning = isModeActive && effect != null &&
                (effect.Id == 8 || effect.Id == 10);

            if (!isSoftwareBreathRunning)
            {
                KeyboardBackLight.SetBrightnessRealtime(val);
            }

            if (isModeActive && effect != null && effect.IsIteHardwareEffect && IteHidController.IsDevicePresent)
            {
                IteHidController.SetHardwareAnimation(
                    effect.IteAnimMode,
                    KeyboardEffect.GetIteSpeedByte(),
                    IteHidController.ScaleBrightness(val),
                    effect.IteBehaviour);
            }

            if (LightBarCheckBox?.IsChecked == true && LedColorCanvas.SelectedColor.HasValue)
            {
                Color c = LedColorCanvas.SelectedColor.Value;
                byte lbBri = (byte)Math.Round(val / 255.0 * 100.0);
                KeyboardBackLight.SetLightBarColor(c.R, c.G, c.B, lbBri);
            }

            // Debounce persistence write to ACPI
            _saveSettingsTimer.Stop();
            _saveSettingsTimer.Start();
        }

        private void PresetColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                try
                {
                    Color col = (Color)ColorConverter.ConvertFromString(hex);

                    // Stop animated effects when applying a static preset color
                    if (ModeStatus.IsChecked == true)
                    {
                        ModeStatus.IsChecked = false;
                    }

                    if (LedStatusCheckBox.IsChecked != true)
                    {
                        LedStatusCheckBox.IsChecked = true;
                    }

                    _pendingColor = null;
                    LedColorCanvas.SelectedColor = col;

                    KeyboardBackLight.SetColour(col.R, col.G, col.B);
                    KeyboardBackLight.SetBrightness(_pendingBrightness);

                    if (LightBarCheckBox?.IsChecked == true)
                    {
                        byte lbBri = (byte)Math.Round(_pendingBrightness / 255.0 * 100.0);
                        KeyboardBackLight.SetLightBarColor(col.R, col.G, col.B, lbBri);
                    }
                }
                catch
                {
                }
            }
        }

        private void UpdateAvailableEffects()
        {
            _isUpdatingEffects = true;
            try
            {
                bool isBattery = KeyboardEffects.IsRunningOnBattery;
                bool allowSoftware = SoftwareEffectsSwitch?.IsChecked == true && !isBattery;
                KeyboardEffect[] availableEffects = KeyboardEffects.GetAvailableEffects(_caps, allowSoftware);
                EffectsListBox.ItemsSource = availableEffects;

                bool hasEffects = availableEffects != null && availableEffects.Length > 0;
                if (NoEffectsMessage != null)
                {
                    if (isBattery)
                    {
                        NoEffectsMessage.Text = "Software animations are disabled on battery power to conserve battery life.";
                    }
                    else
                    {
                        NoEffectsMessage.Text = "Turn on Software Lighting Effects above to view extra animations.";
                    }
                    NoEffectsMessage.Visibility = hasEffects ? Visibility.Collapsed : Visibility.Visible;
                }

                if (AnimationSpeedRow != null)
                {
                    AnimationSpeedRow.IsEnabled = hasEffects;
                    AnimationSpeedRow.Opacity = hasEffects ? 1.0 : 0.55;
                }

                if (hasEffects && EffectsListBox.SelectedIndex < 0)
                {
                    EffectsListBox.SelectedIndex = 0;
                }
            }
            finally
            {
                _isUpdatingEffects = false;
            }
        }

        private void SoftwareEffectsSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            if (KeyboardEffects.IsRunningOnBattery)
            {
                SoftwareEffectsSwitch.IsChecked = false;
                UpdateBatterySoftwareAnimationState(false);
                return;
            }

            bool allowSoftware = SoftwareEffectsSwitch.IsChecked == true;

            try
            {
                Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "SoftwareEffects", allowSoftware ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
            }
            catch
            {
            }

            // If software animations are disabled and an active software effect is playing, turn off effect
            if (!allowSoftware && EffectsListBox.SelectedItem is KeyboardEffect active && active.IsSoftware)
            {
                ModeStatus.IsChecked = false;
                SaveActiveEffect(null, false);
            }

            UpdateAvailableEffects();
            UpdateColorControlsState();
            UpdateBootEffectControlState();
        }

        private void EffectsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _isUpdatingEffects) return;

            if (EffectsListBox.SelectedItem is KeyboardEffect effect)
            {
                if (effect.IsSoftware && KeyboardEffects.IsRunningOnBattery)
                {
                    KeyboardEffect.StopAllSoftwareEffects();
                    SetSleepTimerPaused(false);
                    UpdateColorControlsState();
                    UpdateBootEffectControlState();
                    return;
                }

                if (ModeStatus.IsChecked != true)
                {
                    ModeStatus.IsChecked = true;
                }
                else
                {
                    ApplyCurrentEffect();
                }
                SaveActiveEffect(effect, ModeStatus.IsChecked == true);
            }

            UpdateColorControlsState();
            UpdateBootEffectControlState();
        }

        private void ModeStatus_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isUpdatingEffects) return;

            if (LedStatusCheckBox.IsChecked != true)
            {
                LedStatusCheckBox.IsChecked = true;
            }

            if (EffectsListBox.SelectedItem is KeyboardEffect effect && effect.IsSoftware && KeyboardEffects.IsRunningOnBattery)
            {
                UpdateColorControlsState();
                UpdateBootEffectControlState();
                return;
            }

            if (EffectsListBox.SelectedIndex < 0)
            {
                EffectsListBox.SelectedIndex = 0;
            }
            else
            {
                ApplyCurrentEffect();
            }

            if (EffectsListBox.SelectedItem is KeyboardEffect selectedEffect)
            {
                SaveActiveEffect(selectedEffect, true);
            }
            UpdateColorControlsState();
            UpdateBootEffectControlState();
        }

        private void ModeStatus_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isUpdatingEffects) return;

            KeyboardEffect.StopAllSoftwareEffects();
            var status = KeyboardBackLight.GetStatus();
            if (status.Effect?.Timer != null)
            {
                status.Effect.Timer.Stop();
            }

            SetSleepTimerPaused(false);
            if (!KeyboardEffects.IsRunningOnBattery)
            {
                SaveActiveEffect(null, false);
            }

            // Restore active static color
            if (LedColorCanvas.SelectedColor.HasValue)
            {
                Color col = LedColorCanvas.SelectedColor.Value;
                KeyboardBackLight.SetColour(col.R, col.G, col.B);
            }
            KeyboardBackLight.SetBrightnessRealtime(_pendingBrightness);

            UpdateColorControlsState();
            UpdateBootEffectControlState();
        }

        private void LedStatusCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            KeyboardBackLight.TurnOn();
            KeyboardBackLight.SetBrightness(_pendingBrightness);

            if (ModeStatus.IsChecked == true)
            {
                ApplyCurrentEffect();
            }
            else if (LedColorCanvas.SelectedColor.HasValue)
            {
                Color c = LedColorCanvas.SelectedColor.Value;
                KeyboardBackLight.SetColour(c.R, c.G, c.B);
                KeyboardBackLight.SetBrightnessRealtime(_pendingBrightness);
            }

            UpdateColorControlsState();
            UpdateBootEffectControlState();
        }

        private void LedStatusCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            KeyboardEffect.StopAllSoftwareEffects();
            SetSleepTimerPaused(false);
            KeyboardBackLight.TurnOff();

            UpdateColorControlsState();
            UpdateBootEffectControlState();
        }

        private void ApplyCurrentEffect()
        {
            if (!(EffectsListBox.SelectedItem is KeyboardEffect effect))
            {
                return;
            }

            if (effect.IsSoftware && KeyboardEffects.IsRunningOnBattery)
            {
                return;
            }

            int speed = KeyboardEffect.CurrentAnimationSpeed;
            KeyboardEffects.Duration = speed == 1 ? 5000 : (speed == 3 ? 1000 : 2500);

            KeyboardEffects.MaxBrightness = _pendingBrightness;

            if (effect.Id == 8 && LedColorCanvas?.SelectedColor.HasValue == true)
            {
                Color c = LedColorCanvas.SelectedColor.Value;
                KeyboardBackLight.SetColourRealtime(c.R, c.G, c.B);
            }

            bool isSoftwareBreath = effect.Id == 8 || effect.Id == 10;
            KeyboardBackLight.TurnOn(isSoftwareBreath ? (byte)0 : _pendingBrightness);

            SetSleepTimerPaused(true);
            effect.SetEffect();
            UpdateColorControlsState();
        }

        private void LightBarCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            byte lbBri = (byte)Math.Round(_pendingBrightness / 255.0 * 100.0);
            if (LedColorCanvas.SelectedColor.HasValue)
            {
                Color c = LedColorCanvas.SelectedColor.Value;
                KeyboardBackLight.SetLightBarColor(c.R, c.G, c.B, lbBri);
            }
            else
            {
                KeyboardBackLight.SetLightBarColor(59, 130, 246, lbBri);
            }
        }

        private void LightBarCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            KeyboardBackLight.SetLightBarOff();
        }

        private void BootEffectCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            KeyboardBackLight.OverrideBootEffect(true);
        }

        private void BootEffectCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            KeyboardBackLight.OverrideBootEffect(false);
        }

                private void SpeedBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int speed))
            {
                ApplyAnimationSpeed(speed);
            }
        }

        private void ApplyAnimationSpeed(int speed)
        {
            KeyboardEffect.CurrentAnimationSpeed = speed;
            UpdateSpeedButtonsUI(speed);

            try
            {
                Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbAnimationSpeed", speed, Microsoft.Win32.RegistryValueKind.DWord);
            }
            catch
            {
            }

            Capabilities.SetKeyboardAnimationSpeed(speed);

            if (EffectsListBox?.SelectedItem is KeyboardEffect effect)
            {
                if (effect.IsIteHardwareEffect && IteHidController.IsDevicePresent)
                {
                    byte iteSpeed = KeyboardEffect.GetIteSpeedByte();
                    IteHidController.SetHardwareAnimation(effect.IteAnimMode, iteSpeed, IteHidController.ScaleBrightness(_pendingBrightness), effect.IteBehaviour);
                }
                else if (effect.IsSoftware)
                {
                    KeyboardEffects.Duration = speed == 1 ? 5000 : (speed == 3 ? 1000 : 2500);
                }
            }
        }

        private void UpdateSpeedButtonsUI(int activeSpeed)
        {
            if (SpeedSlowBtn == null || SpeedNormalBtn == null || SpeedFastBtn == null) return;
            SpeedSlowBtn.Style = activeSpeed == 1 ? (Style)FindResource("AmoledAccentButtonStyle") : (Style)FindResource("AmoledButtonStyle");
            SpeedNormalBtn.Style = activeSpeed == 2 ? (Style)FindResource("AmoledAccentButtonStyle") : (Style)FindResource("AmoledButtonStyle");
            SpeedFastBtn.Style = activeSpeed == 3 ? (Style)FindResource("AmoledAccentButtonStyle") : (Style)FindResource("AmoledButtonStyle");
        }

        private void UpdateBootEffectControlState()
        {
            bool bootOverrideSupported = _caps != null && _caps.SupportsBootEffectOverride;
            if (!bootOverrideSupported)
            {
                if (BootEffectCheckBox != null)
                {
                    BootEffectCheckBox.IsEnabled = false;
                    BootEffectCheckBox.ToolTip = "Not supported on this model";
                }
                if (BootEffectRow != null) BootEffectRow.Opacity = 0.55;
                if (BootEffectBadge != null) BootEffectBadge.Visibility = Visibility.Visible;
                return;
            }

            if (BootEffectBadge != null) BootEffectBadge.Visibility = Visibility.Collapsed;

            if (BootEffectCheckBox != null)
            {
                BootEffectCheckBox.IsEnabled = true;
                BootEffectCheckBox.ToolTip = null;
            }
            if (BootEffectRow != null) BootEffectRow.Opacity = 1.0;
        }

        private void SaveActiveEffect(KeyboardEffect effect, bool active)
        {
            try
            {
                Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectActive", active ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                if (effect != null)
                {
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectId", effect.Id, Microsoft.Win32.RegistryValueKind.DWord);
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectUniqId", (int)effect.UniqId, Microsoft.Win32.RegistryValueKind.DWord);
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectName", effect.Name ?? string.Empty);
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectIsSoftware", effect.IsSoftware ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                }
            }
            catch
            {
            }
        }

        private void LogoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            Capabilities.SetWMI(103, 1u);
        }

        private void LogoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            Capabilities.SetWMI(103, 0u);
        }

        private void TouchpadLedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            Capabilities.SetWMI(103, 1u);
        }

        private void TouchpadLedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            Capabilities.SetWMI(103, 0u);
        }

        private void KeyboarSleepTimer_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || ModeStatus.IsChecked == true) return;

            if (sender is TextBox tb && int.TryParse(tb.Text, out int val))
            {
                if (val > 59)
                {
                    tb.Text = "59";
                }
            }

            int h = int.TryParse(KeyboarSleepTimerHours?.Text, out int parsedH) ? parsedH : 0;
            int m = int.TryParse(KeyboarSleepTimerMunites?.Text, out int parsedM) ? parsedM : 0;
            int s = int.TryParse(KeyboarSleepTimerSeconds?.Text, out int parsedS) ? parsedS : 0;

            int totalSeconds = (h * 3600) + (m * 60) + s;
            if (totalSeconds > 0)
            {
                KeyboardBackLight.SetSleepTimer(totalSeconds);
            }
            else
            {
                KeyboardBackLight.TurnSleepTimerOff();
            }
        }

        private void SetSleepTimerPaused(bool paused)
        {
            if (KeyboarSleepTimerHours == null || KeyboarSleepTimerMunites == null || KeyboarSleepTimerSeconds == null)
            {
                return;
            }

            KeyboarSleepTimerHours.IsEnabled = !paused;
            KeyboarSleepTimerMunites.IsEnabled = !paused;
            KeyboarSleepTimerSeconds.IsEnabled = !paused;

            if (paused)
            {
                KeyboardBackLight.TurnSleepTimerOff(updateRegistry: false);
            }
            else
            {
                int h = int.TryParse(KeyboarSleepTimerHours?.Text, out int parsedH) ? parsedH : 0;
                int m = int.TryParse(KeyboarSleepTimerMunites?.Text, out int parsedM) ? parsedM : 0;
                int s = int.TryParse(KeyboarSleepTimerSeconds?.Text, out int parsedS) ? parsedS : 0;

                int totalSeconds = (h * 3600) + (m * 60) + s;
                if (totalSeconds > 0)
                {
                    KeyboardBackLight.SetSleepTimer(totalSeconds);
                }
                else
                {
                    KeyboardBackLight.TurnSleepTimerOff(updateRegistry: true);
                }
            }
        }

        private void SyncPowerBadgeUI(bool isAc)
        {
            if (SoftwareBatteryBadge != null)
            {
                SoftwareBatteryBadge.Visibility = isAc ? Visibility.Collapsed : Visibility.Visible;
            }
            if (SoftwareEffectsSwitch != null)
            {
                SoftwareEffectsSwitch.IsEnabled = isAc;
                if (!isAc)
                {
                    SoftwareEffectsSwitch.IsChecked = false;
                }
            }
        }

        public void UpdateBatterySoftwareAnimationState(bool isAcOnline)
        {
            Dispatcher.Invoke(() =>
            {
                _isUpdatingEffects = true;
                try
                {
                    _lastAcStateInUI = isAcOnline;

                    if (!isAcOnline)
                    {
                        if (SoftwareBatteryBadge != null) SoftwareBatteryBadge.Visibility = Visibility.Visible;
                        if (SoftwareAnimationSubtitle != null) SoftwareAnimationSubtitle.Visibility = Visibility.Visible;
                        if (SoftwareEffectsSwitch != null)
                        {
                            SoftwareEffectsSwitch.IsEnabled = false;
                            SoftwareEffectsSwitch.IsChecked = false;
                        }

                        if (EffectsListBox?.SelectedItem is KeyboardEffect effect && effect.IsSoftware)
                        {
                            KeyboardEffect.StopAllSoftwareEffects();
                            if (LedColorCanvas.SelectedColor.HasValue)
                            {
                                Color c = LedColorCanvas.SelectedColor.Value;
                                KeyboardBackLight.SetBrightnessRealtime(_pendingBrightness);
                                KeyboardBackLight.SetColour(c.R, c.G, c.B);
                            }
                            SetSleepTimerPaused(false);
                        }

                        UpdateAvailableEffects();
                    }
                    else
                    {
                        if (SoftwareBatteryBadge != null) SoftwareBatteryBadge.Visibility = Visibility.Collapsed;
                        if (SoftwareAnimationSubtitle != null) SoftwareAnimationSubtitle.Visibility = Visibility.Visible;
                        if (SoftwareEffectsSwitch != null)
                        {
                            SoftwareEffectsSwitch.IsEnabled = true;
                            object reg = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "SoftwareEffects", 1);
                            SoftwareEffectsSwitch.IsChecked = reg == null || Convert.ToInt32(reg) != 0;
                        }

                        UpdateAvailableEffects();

                        object regId = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectId", null);
                        if (regId is int idVal && EffectsListBox.ItemsSource is KeyboardEffect[] available)
                        {
                            for (int i = 0; i < available.Length; i++)
                            {
                                if (available[i].Id == idVal)
                                {
                                    EffectsListBox.SelectedIndex = i;
                                    break;
                                }
                            }
                        }

                        object regActive = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "KbEffectActive", null);
                        bool shouldBeActive = regActive is int a && a == 1;
                        if (shouldBeActive)
                        {
                            ModeStatus.IsChecked = true;
                        }
                    }

                    UpdateColorControlsState();
                    UpdateBootEffectControlState();
                }
                finally
                {
                    _isUpdatingEffects = false;
                }
            });
        }

        private void UpdateColorControlsState()
        {
            if (LedStatusCheckBox?.IsChecked != true)
            {
                SetColorControlsEnabled(false, "Keyboard backlight is turned off");
                return;
            }

            bool isModeActive = ModeStatus?.IsChecked == true;
            KeyboardEffect effect = EffectsListBox?.SelectedItem as KeyboardEffect;

            bool isSelfColoringEffect = isModeActive && effect != null &&
                (effect.Id == 9 ||
                 effect.Id == 10 ||
                 effect.Id == 3 ||
                 effect.Id == 1 ||
                 (!string.IsNullOrEmpty(effect.Name) &&
                  (effect.Name.IndexOf("Cycle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   effect.Name.IndexOf("Colorful", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   effect.Name.IndexOf("Rainbow", StringComparison.OrdinalIgnoreCase) >= 0)));

            if (isSelfColoringEffect)
            {
                SetColorControlsEnabled(false, $"Color selection is disabled while '{effect.Name}' animation is active because it cycles colors automatically.");
            }
            else
            {
                SetColorControlsEnabled(true, null);
            }
        }

        private void SetColorControlsEnabled(bool isEnabled, string tooltipReason)
        {
            if (QuickColorPresetsCard != null)
            {
                QuickColorPresetsCard.IsEnabled = isEnabled;
                QuickColorPresetsCard.Opacity = isEnabled ? 1.0 : 0.45;
                QuickColorPresetsCard.ToolTip = tooltipReason;
            }

            if (ColorCanvasCard != null)
            {
                ColorCanvasCard.IsEnabled = isEnabled;
                ColorCanvasCard.Opacity = isEnabled ? 1.0 : 0.45;
                ColorCanvasCard.ToolTip = tooltipReason;
            }
        }

        private bool HideAlphaChannelFromCanvas(DependencyObject parent)
        {
            if (parent == null) return false;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBlock tb)
                {
                    string txt = tb.Text?.Trim();
                    if (string.Equals(txt, "Alpha", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(txt, "A", StringComparison.Ordinal))
                    {
                        DependencyObject current = tb;
                        while (current != null && !(current is Grid) && current != LedColorCanvas)
                        {
                            current = VisualTreeHelper.GetParent(current);
                        }

                        if (current is Grid channelGrid)
                        {
                            channelGrid.Visibility = Visibility.Collapsed;
                            return true;
                        }
                    }
                }

                if (HideAlphaChannelFromCanvas(child))
                {
                    return true;
                }
            }

            return false;
        }
    
        private void KeyboarSleepTimer_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Escape)
            {
                (Window.GetWindow(sender as DependencyObject ?? this) ?? Application.Current.MainWindow)?.Focus();
                e.Handled = true;
            }
        }

        private void KeyboarSleepTimer_LostFocus(object sender, RoutedEventArgs e)
        {
            KeyboarSleepTimer_TextChanged(sender, null);
        }

        internal void SyncFromHardwareHotkey()
        {
            bool prevInit = _isInitializing;
            _isInitializing = true;
            try
            {
                KeyboardStatus status = KeyboardBackLight.GetStatus();
                LedStatusCheckBox.IsChecked = status.LightState;
                if (status.LightState && status.BackLight.A > 0)
                {
                    int step = KeyboardBackLight.BrightnessToStep(status.BackLight.A, status.LightState);
                    if (step < 1) step = 1;
                    _pendingBrightness = (byte)(step == 1 ? 64 : (step == 2 ? 128 : (step == 3 ? 192 : 255)));
                    KeyboardEffects.MaxBrightness = _pendingBrightness;
                    KeyboardBrightnessSlider.Value = step;
                    BrightnessPercentText.Text = $"{step * 25}%";
                }
                if (_caps.SupportsRgb)
                {
                    LedColorCanvas.SelectedColor = Color.FromRgb(status.BackLight.R, status.BackLight.G, status.BackLight.B);
                }
            }
            catch
            {
            }
            finally
            {
                _isInitializing = prevInit;
            }
        }

        private void StyleColorCanvasElements(DependencyObject parent)
        {
            if (parent == null) return;

            var bgBrush = new SolidColorBrush(Color.FromRgb(16, 16, 20));
            var borderBrush = new SolidColorBrush(Color.FromRgb(30, 30, 38));

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is Button btn)
                {
                    btn.Background = bgBrush;
                    btn.BorderBrush = borderBrush;
                }
                else if (child is Border b && b.Background is SolidColorBrush scb && scb.Color == Colors.Black)
                {
                    b.Background = bgBrush;
                }
                else if (child is Panel p && p.Background is SolidColorBrush pscb && pscb.Color == Colors.Black)
                {
                    p.Background = bgBrush;
                }
                else if (child is TextBox tb)
                {
                    tb.Background = bgBrush;
                    tb.Foreground = Brushes.White;
                }
                else if (child is TextBlock textBlock)
                {
                    textBlock.Foreground = Brushes.White;
                }
                else if (child is Label lbl)
                {
                    lbl.Foreground = Brushes.White;
                }
                else if (child is Control ctrl && !(child is Button))
                {
                    ctrl.Foreground = Brushes.White;
                }

                StyleColorCanvasElements(child);
            }
        }
    }
}
