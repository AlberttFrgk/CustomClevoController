using BSCustomClevoController.Utility;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using BatteryChargeStatus = BSCustomClevoController.Controls.BatteryChargeStatus;
using BSCustomClevoController.Controls;


namespace BSCustomClevoController.Views
{
    public partial class Page_BatterySettings : Page
    {
        private readonly DispatcherTimer _liveBatteryTimer;
        private readonly DispatcherTimer _statusClearTimer;
        private bool _isInitializing = false;

        public Page_BatterySettings()
        {
            InitializeComponent();
            LoadBatterySettings();
            SyncLiveBatteryStatus();

            _liveBatteryTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _liveBatteryTimer.Tick += (s, e) => SyncLiveBatteryStatus();
            _liveBatteryTimer.Start();

            _statusClearTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _statusClearTimer.Tick += (s, e) =>
            {
                _statusClearTimer.Stop();
                BatteryStatusText.Text = "";
            };
        }

        private void LoadBatterySettings()
        {
            _isInitializing = true;
            try
            {
                BatteryChargeStatus status = BatteryControl.GetStatus();

                if (status.Mode == BatteryChargeMode.Recommended)
                {
                    RecommendedChargeRadio.IsChecked = true;
                }
                else if (status.Mode == BatteryChargeMode.Custom)
                {
                    CustomChargeRadio.IsChecked = true;
                }
                else
                {
                    MaxChargeRadio.IsChecked = true;
                }

                CustomStartBox.Text = status.StartThreshold.ToString();
                CustomStopBox.Text = status.StopThreshold.ToString();
                UpdateThresholdPanelState();
            }
            catch
            {
                MaxChargeRadio.IsChecked = true;
                UpdateThresholdPanelState();
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void SyncLiveBatteryStatus()
        {
            try
            {
                PowerStatus power = SystemInformation.PowerStatus;
                int percent = (int)Math.Round(power.BatteryLifePercent * 100);
                percent = Math.Max(0, Math.Min(100, percent));

                bool isCharging = (power.BatteryChargeStatus & System.Windows.Forms.BatteryChargeStatus.Charging) != 0;
                bool isAc = power.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;

                LiveBatteryPercentText.Text = percent + "%";
                LiveBatteryProgressBar.Value = percent;

                // Dynamic battery health indicator color
                SolidColorBrush indicatorBrush;
                if (percent <= 20)
                {
                    indicatorBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red / Low
                }
                else if (percent <= 45)
                {
                    indicatorBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber / Moderate
                }
                else if (isCharging)
                {
                    indicatorBrush = new SolidColorBrush(Color.FromRgb(27, 161, 226)); // Electric Cyan / Charging
                }
                else
                {
                    indicatorBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Emerald Green / Healthy
                }

                LiveBatteryPercentText.Foreground = indicatorBrush;
                LiveBatteryProgressBar.Foreground = indicatorBrush;

                if (isAc)
                {
                    if (isCharging)
                    {
                        LivePowerSourceText.Text = "AC Connected (Charging)";
                        LivePowerSourceText.Foreground = new SolidColorBrush(Color.FromRgb(27, 161, 226));
                    }
                    else if (percent >= 99)
                    {
                        LivePowerSourceText.Text = "AC Connected (Fully Charged)";
                        LivePowerSourceText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    }
                    else
                    {
                        LivePowerSourceText.Text = "AC Connected (Plugged In)";
                        LivePowerSourceText.Foreground = (SolidColorBrush)FindResource("Amoled.TextPrimary");
                    }
                }
                else
                {
                    LivePowerSourceText.Text = "On Battery (Discharging)";
                    LivePowerSourceText.Foreground = (percent <= 20)
                        ? new SolidColorBrush(Color.FromRgb(239, 68, 68))
                        : (SolidColorBrush)FindResource("Amoled.TextPrimary");
                }

                BatteryChargeStatus status = BatteryControl.GetStatus();
                if (!status.Enabled || status.Mode == BatteryChargeMode.Maximum)
                {
                    LiveLimitStatusText.Text = "Disabled (Full 100% Charge)";
                    LiveLimitStatusText.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                }
                else if (status.Mode == BatteryChargeMode.Recommended)
                {
                    LiveLimitStatusText.Text = "Recommended (70% - 80%)";
                    LiveLimitStatusText.Foreground = new SolidColorBrush(Color.FromRgb(27, 161, 226));
                }
                else
                {
                    LiveLimitStatusText.Text = string.Format("Custom ({0}% - {1}%)", status.StartThreshold, status.StopThreshold);
                    LiveLimitStatusText.Foreground = new SolidColorBrush(Color.FromRgb(27, 161, 226));
                }

                // Query live battery telemetry (voltage & wattage)
                BatteryTelemetry telemetry = BatteryControl.GetTelemetry();

                if (telemetry.VoltageMv > 0)
                {
                    double volts = telemetry.VoltageMv / 1000.0;
                    LiveVoltageText.Text = volts.ToString("0.00") + " V";
                }
                else
                {
                    LiveVoltageText.Text = "-- V";
                }

                if (telemetry.ChargeRateMw > 0)
                {
                    double watts = telemetry.ChargeRateMw / 1000.0;
                    LiveChargeRateText.Text = string.Format("+{0:0.0} W (Charging)", watts);
                    LiveChargeRateText.Foreground = new SolidColorBrush(Color.FromRgb(27, 161, 226));
                }
                else if (isCharging)
                {
                    LiveChargeRateText.Text = "Charging";
                    LiveChargeRateText.Foreground = new SolidColorBrush(Color.FromRgb(27, 161, 226));
                }
                else if (telemetry.DischargeRateMw > 0)
                {
                    double watts = telemetry.DischargeRateMw / 1000.0;
                    LiveChargeRateText.Text = string.Format("-{0:0.0} W (Discharging)", watts);
                    LiveChargeRateText.Foreground = (SolidColorBrush)FindResource("Amoled.TextSecondary");
                }
                else
                {
                    LiveChargeRateText.Text = isAc ? "0.0 W (Idle)" : "0.0 W";
                    LiveChargeRateText.Foreground = (SolidColorBrush)FindResource("Amoled.TextMuted");
                }
            }
            catch
            {
            }
        }

        private void ChargeRadio_Checked(object sender, RoutedEventArgs e)
        {
            UpdateThresholdPanelState();
            if (!_isInitializing)
            {
                ApplyCurrentSettings();
            }
        }

        private void CustomThresholdBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!_isInitializing && CustomChargeRadio != null && CustomChargeRadio.IsChecked == true)
            {
                ApplyCurrentSettings();
            }
        }

                private void CustomThresholdBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (!_isInitializing && CustomChargeRadio != null && CustomChargeRadio.IsChecked == true)
                {
                    ApplyCurrentSettings();
                }
                (Window.GetWindow(sender as DependencyObject ?? this) ?? System.Windows.Application.Current.MainWindow)?.Focus();
                e.Handled = true;
            }
        }

        private void UpdateThresholdPanelState()
        {
            if (CustomThresholdPanel == null) return;
            bool isCustom = CustomChargeRadio != null && CustomChargeRadio.IsChecked == true;
            CustomStartBox.IsEnabled = isCustom;
            CustomStopBox.IsEnabled = isCustom;
            CustomThresholdPanel.Opacity = isCustom ? 1.0 : 0.45;
        }

        private void ApplyCurrentSettings()
        {
            try
            {
                _statusClearTimer.Stop();

                BatteryChargeMode selectedMode;
                int start = 90;
                int stop = 100;

                if (MaxChargeRadio.IsChecked == true)
                {
                    selectedMode = BatteryChargeMode.Maximum;
                }
                else if (RecommendedChargeRadio.IsChecked == true)
                {
                    selectedMode = BatteryChargeMode.Recommended;
                }
                else
                {
                    selectedMode = BatteryChargeMode.Custom;

                    if (!int.TryParse(CustomStartBox.Text.Trim(), out start) || start < 40 || start > 99)
                    {
                        ShowStatusMessage("Start percentage must be between 40% and 99%.", isError: true);
                        return;
                    }

                    if (!int.TryParse(CustomStopBox.Text.Trim(), out stop) || stop < 50 || stop > 100)
                    {
                        ShowStatusMessage("Stop percentage must be between 50% and 100%.", isError: true);
                        return;
                    }

                    if (start >= stop)
                    {
                        ShowStatusMessage("Start percentage must be lower than stop percentage.", isError: true);
                        return;
                    }
                }

                bool success = BatteryControl.SetChargeMode(selectedMode, start, stop);
                if (success)
                {
                    ShowStatusMessage("Battery settings applied.", isError: false);
                    SyncLiveBatteryStatus();
                    _statusClearTimer.Start();
                }
                else
                {
                    ShowStatusMessage("Failed to apply battery settings to EC.", isError: true);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage("Error: " + ex.Message, isError: true);
            }
        }

        private void ShowStatusMessage(string message, bool isError)
        {
            BatteryStatusText.Text = message;
            BatteryStatusText.Foreground = isError
                ? new SolidColorBrush(Color.FromRgb(239, 68, 68))
                : new SolidColorBrush(Color.FromRgb(16, 185, 129));
        }
    }
}
