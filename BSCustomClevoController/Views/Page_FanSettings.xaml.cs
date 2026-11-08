using BSCustomClevoController.Utility;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static BSCustomClevoController.Utility.Structs;
using BSCustomClevoController.Controls;


namespace BSCustomClevoController.Views
{
    public partial class Page_FanSettings : Page
    {
        private readonly SolidColorBrush colorBlue = new SolidColorBrush(Color.FromRgb(56, 189, 248));
        private readonly SolidColorBrush colorSky = new SolidColorBrush(Color.FromRgb(61, 184, 245));
        private readonly SolidColorBrush colorYellow = new SolidColorBrush(Color.FromRgb(255, 178, 36));
        private readonly SolidColorBrush colorRed = new SolidColorBrush(Color.FromRgb(255, 77, 77));
        private readonly SolidColorBrush colorDefaultStatus = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        private readonly SolidColorBrush colorActiveBorder = new SolidColorBrush(Color.FromRgb(0, 120, 215));
        private readonly SolidColorBrush colorInactiveBorder = new SolidColorBrush(Color.FromRgb(60, 60, 70));
        private readonly SolidColorBrush colorActiveBg = new SolidColorBrush(Color.FromRgb(36, 48, 64));
        private readonly SolidColorBrush colorDefaultBg = new SolidColorBrush(Color.FromRgb(42, 42, 48));
        private SolidColorBrush _cpuPrimaryBrush = new SolidColorBrush(Color.FromRgb(61, 184, 245));
        private SolidColorBrush _gpuPrimaryBrush = new SolidColorBrush(Color.FromRgb(61, 214, 140));
        private readonly DispatcherTimer _rpmTimer;
        private readonly DispatcherTimer _curveStatusResetTimer;

        private const string IntelLogoPath = "M20.42 7.345v9.18h1.651v-9.18zM0 7.475v1.737h1.737V7.474zm9.78.352v6.053c0 .513.044.945.13 1.292.087.34.235.618.44.828.203.21.475.359.803.451.334.093.754.136 1.255.136h.216v-1.533c-.24 0-.445-.012-.593-.037a.672.672 0 0 1-.39-.173.693.693 0 0 1-.173-.377 4.002 4.002 0 0 1-.037-.606v-2.182h1.193v-1.416h-1.193V7.827zm-3.505 2.312c-.396 0-.76.08-1.082.241-.327.161-.6.384-.822.668l-.087.117v-.902H2.658v6.256h1.639v-3.214c.018-.588.16-1.02.433-1.299.29-.297.642-.445 1.044-.445.476 0 .841.149 1.082.433.235.284.359.686.359 1.2v3.324h1.663V12.97c.006-.89-.229-1.595-.686-2.09-.458-.495-1.1-.742-1.917-.742zm10.065.006a3.252 3.252 0 0 0-2.306.946c-.29.29-.525.637-.692 1.033a3.145 3.145 0 0 0-.254 1.273c0 .452.08.878.241 1.274.161.395.39.742.674 1.032.284.29.637.526 1.045.693.408.173.86.26 1.342.26 1.397 0 2.262-.637 2.782-1.23l-1.187-.904c-.248.297-.841.699-1.583.699-.464 0-.847-.105-1.138-.321a1.588 1.588 0 0 1-.593-.872l-.019-.056h4.915v-.587c0-.451-.08-.872-.235-1.267a3.393 3.393 0 0 0-.661-1.033 3.013 3.013 0 0 0-1.02-.692 3.345 3.345 0 0 0-1.311-.248zm-16.297.118v6.256h1.651v-6.256zm16.278 1.286c1.132 0 1.664.797 1.664 1.255l-3.32.006c0-.458.525-1.255 1.656-1.261z";
        private const string AmdLogoPath = "M18.324 9.137l1.559 1.56h2.556v2.557L24 14.814V9.137zM2 9.52l-2 4.96h1.309l.37-.982H3.9l.408.982h1.338L3.432 9.52zm4.209 0v4.955h1.238v-3.092l1.338 1.562h.188l1.338-1.556v3.091h1.238V9.52H10.47l-1.592 1.845L7.287 9.52zm6.283 0v4.96h2.057c1.979 0 2.88-1.046 2.88-2.472 0-1.36-.937-2.488-2.747-2.488zm1.237.91h.792c1.17 0 1.63.711 1.63 1.57 0 .728-.372 1.572-1.616 1.572h-.806zm-10.985.273l.791 1.932H2.008zm17.137.307l-1.604 1.603v2.25h2.246l1.604-1.607h-2.246z";
        private const string NvidiaLogoPath = "M8.948 8.798v-1.43a6.7 6.7 0 0 1 .424-.018c3.922-.124 6.493 3.374 6.493 3.374s-2.774 3.851-5.75 3.851c-.398 0-.787-.062-1.158-.185v-4.346c1.528.185 1.837.857 2.747 2.385l2.04-1.714s-1.492-1.952-4-1.952a6.016 6.016 0 0 0-.796.035m0-4.735v2.138l.424-.027c5.45-.185 9.01 4.47 9.01 4.47s-4.08 4.964-8.33 4.964c-.37 0-.733-.035-1.095-.097v1.325c.3.035.61.062.91.062 3.957 0 6.82-2.023 9.593-4.408.459.371 2.34 1.263 2.73 1.652-2.633 2.208-8.772 3.984-12.253 3.984-.335 0-.653-.018-.971-.053v1.864H24V4.063zm0 10.326v1.131c-3.657-.654-4.673-4.46-4.673-4.46s1.758-1.944 4.673-2.262v1.237H8.94c-1.528-.186-2.73 1.245-2.73 1.245s.68 2.412 2.739 3.11M2.456 10.9s2.164-3.197 6.5-3.533V6.201C4.153 6.59 0 10.653 0 10.653s2.35 6.802 8.948 7.42v-1.237c-4.84-.6-6.492-5.936-6.492-5.936z";

        private const string DefaultCurveStatusText = "Points must increase gradually up to 100°C and 100% fan speed.";
        private const string AutoDesc = "Automatically adjusts fan speed based on component temperatures.";
        private const string MaxDesc = "Runs both CPU and GPU fans at full speed (100%) for maximum cooling.";
        private const string MaxQDesc = "Limits fan noise for quiet gaming on supported models.";
        private const string AntiDustDesc = "Spins fans at high speed in cycles to clean out dust.";
        private const string CustomDesc = "Controls fan speeds using your custom temperature curve below.";

                protected override void OnPreviewMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (System.Windows.Input.Keyboard.FocusedElement is DependencyObject focused)
            {
                TextBox textBox = focused as TextBox;
                if (textBox == null)
                {
                    DependencyObject cur = focused;
                    while (cur != null)
                    {
                        if (cur is TextBox tb)
                        {
                            textBox = tb;
                            break;
                        }
                        cur = VisualTreeHelper.GetParent(cur);
                    }
                }

                if (textBox != null)
                {
                    DependencyObject clicked = e.OriginalSource as DependencyObject;
                    bool isInside = false;
                    DependencyObject check = clicked;
                    while (check != null)
                    {
                        if (check == textBox)
                        {
                            isInside = true;
                            break;
                        }
                        check = VisualTreeHelper.GetParent(check);
                    }

                    if (!isInside)
                    {
                        (Window.GetWindow(this) ?? Application.Current.MainWindow)?.Focus();
                    }
                }
            }
        }

        private void FanPoint_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                (Window.GetWindow(sender as DependencyObject ?? this) ?? Application.Current.MainWindow)?.Focus();
                e.Handled = true;
            }
        }

        public Page_FanSettings()
        {
            InitializeComponent();
            ApplyHardwareBranding();
            ApplyFeatureCapabilities();

            FanStatus fanStatus = Fan.GetStatus();

            _rpmTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _rpmTimer.Tick += rpmTimer_Tick;

            _curveStatusResetTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _curveStatusResetTimer.Tick += (s, e) =>
            {
                _curveStatusResetTimer.Stop();
                CurveStatusMessage.Text = DefaultCurveStatusText;
                CurveStatusMessage.Foreground = colorDefaultStatus;
            };

            Loaded += (s, e) =>
            {
                if (!_rpmTimer.IsEnabled) _rpmTimer.Start();
                rpmTimer_Tick(null, EventArgs.Empty);
            };
            Unloaded += (s, e) =>
            {
                if (_rpmTimer.IsEnabled) _rpmTimer.Stop();
                if (_curveStatusResetTimer.IsEnabled) _curveStatusResetTimer.Stop();
            };

            if (!_rpmTimer.IsEnabled)
                _rpmTimer.Start();

            OffsetSlider.Value = fanStatus.Offset;
            OffsetValueText.Text = string.Format("{0}%", (int)OffsetSlider.Value);

            SetCustomFanValues(fanStatus.Fan_CPU, fanStatus.Fan_GPU1);
            SetActiveModeUI(fanStatus.Mode);

            rpmTimer_Tick(null, EventArgs.Empty);
        }

        private void ApplyFeatureCapabilities()
        {
            bool antiDustSupported = Fan.IsAntiDustSupported();
            AntiDustModeBtn.IsEnabled = antiDustSupported;
            if (!antiDustSupported)
            {
                AntiDustModeBtn.ToolTip = "Not supported on this model";
            }

            bool maxQSupported = Fan.IsMaxQSupported();
            MaxQModeBtn.IsEnabled = maxQSupported;
            if (!maxQSupported)
            {
                MaxQModeBtn.ToolTip = "Not supported on this model";
            }
        }

        internal void SetActiveModeUI(Fan.Mode mode, string customMessage = null)
        {
            if (mode == Fan.Mode.MaxQ && !Fan.IsMaxQSupported())
            {
                mode = Fan.Mode.Auto;
            }

            SetButtonActive(AutoModeBtn, mode == Fan.Mode.Auto);
            SetButtonActive(MaxModeBtn, mode == Fan.Mode.Max);
            SetButtonActive(AntiDustModeBtn, false);
            SetButtonActive(MaxQModeBtn, mode == Fan.Mode.MaxQ);
            SetButtonActive(CustomModeBtn, mode == Fan.Mode.Custom);

            bool isCustom = mode == Fan.Mode.Custom;
            SetCustomFanUIElements(isCustom);

            if (!string.IsNullOrEmpty(customMessage))
            {
                FanModeDesText.Text = customMessage;
                return;
            }

            switch (mode)
            {
                case Fan.Mode.Auto:
                    FanModeDesText.Text = AutoDesc;
                    break;
                case Fan.Mode.Max:
                    FanModeDesText.Text = MaxDesc;
                    break;
                case Fan.Mode.MaxQ:
                    FanModeDesText.Text = MaxQDesc;
                    break;
                case Fan.Mode.Custom:
                    FanModeDesText.Text = CustomDesc;
                    break;
                default:
                    FanModeDesText.Text = "Standard cooling profile active.";
                    break;
            }
        }

        private void SetButtonActive(Button btn, bool isActive)
        {
            btn.BorderBrush = isActive ? colorActiveBorder : colorInactiveBorder;
            btn.Background = isActive ? colorActiveBg : colorDefaultBg;
        }

        private void SetCustomFanUIElements(bool active)
        {
            ApplyCustomFan.IsEnabled = active;
            ResetCustomFan.IsEnabled = active;

            CPU_Temp1.IsEnabled = active;
            CPU_Temp2.IsEnabled = active;
            CPU_Temp3.IsEnabled = active;
            CPU_Temp4.IsEnabled = active;
            CPU_Speed1.IsEnabled = active;
            CPU_Speed2.IsEnabled = active;
            CPU_Speed3.IsEnabled = active;
            CPU_Speed4.IsEnabled = active;

            GPU_Temp1.IsEnabled = active;
            GPU_Temp2.IsEnabled = active;
            GPU_Temp3.IsEnabled = active;
            GPU_Temp4.IsEnabled = active;
            GPU_Speed1.IsEnabled = active;
            GPU_Speed2.IsEnabled = active;
            GPU_Speed3.IsEnabled = active;
            GPU_Speed4.IsEnabled = active;
        }

        private void SetCustomFanValues(FanInfo CPU, FanInfo GPU)
        {
            CPU_Temp1.Text = (CPU.T1 > 0 ? CPU.T1 : 40).ToString();
            CPU_Temp2.Text = (CPU.T2 > 0 ? CPU.T2 : 60).ToString();
            CPU_Temp3.Text = (CPU.T3 > 0 ? CPU.T3 : 80).ToString();
            CPU_Temp4.Text = (CPU.T4 > 0 ? CPU.T4 : 100).ToString();
            CPU_Speed1.Text = (CPU.T1 > 0 ? CPU.D1 : (CPU.D1 > 0 ? CPU.D1 : 35)).ToString();
            CPU_Speed2.Text = (CPU.D2 > 0 ? CPU.D2 : 50).ToString();
            CPU_Speed3.Text = (CPU.D3 > 0 ? CPU.D3 : 85).ToString();
            CPU_Speed4.Text = (CPU.D4 > 0 ? CPU.D4 : 100).ToString();

            GPU_Temp1.Text = (GPU.T1 > 0 ? GPU.T1 : 40).ToString();
            GPU_Temp2.Text = (GPU.T2 > 0 ? GPU.T2 : 60).ToString();
            GPU_Temp3.Text = (GPU.T3 > 0 ? GPU.T3 : 80).ToString();
            GPU_Temp4.Text = (GPU.T4 > 0 ? GPU.T4 : 100).ToString();
            GPU_Speed1.Text = (GPU.T1 > 0 ? GPU.D1 : (GPU.D1 > 0 ? GPU.D1 : 35)).ToString();
            GPU_Speed2.Text = (GPU.D2 > 0 ? GPU.D2 : 50).ToString();
            GPU_Speed3.Text = (GPU.D3 > 0 ? GPU.D3 : 85).ToString();
            GPU_Speed4.Text = (GPU.D4 > 0 ? GPU.D4 : 100).ToString();
        }

        private void ApplyHardwareBranding()
        {
            string cpuModel;
            CpuBrand cpuBrand = HardwareDetector.DetectCpu(out cpuModel);

            string gpuModel;
            GpuBrand gpuBrand = HardwareDetector.DetectGpu(out gpuModel);

            // CPU Vendor Branding
            if (cpuBrand == CpuBrand.Amd)
            {
                _cpuPrimaryBrush = new SolidColorBrush(Color.FromRgb(255, 77, 77));
                CpuVendorLogo.Data = Geometry.Parse(AmdLogoPath);
                CpuTelemetryCard.BorderBrush = new SolidColorBrush(Color.FromRgb(48, 25, 28));
            }
            else
            {
                _cpuPrimaryBrush = new SolidColorBrush(Color.FromRgb(61, 184, 245));
                CpuVendorLogo.Data = Geometry.Parse(IntelLogoPath);
                CpuTelemetryCard.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 38, 56));
            }

            CpuVendorLogo.Fill = _cpuPrimaryBrush;
            CpuFanBlades.Fill = _cpuPrimaryBrush;
            RpmCpuText.Foreground = _cpuPrimaryBrush;
            if (!string.IsNullOrEmpty(cpuModel) && cpuModel != "CPU")
            {
                CpuSubtitleText.Text = cpuModel;
            }

            // GPU Vendor Branding
            if (gpuBrand == GpuBrand.Amd)
            {
                _gpuPrimaryBrush = new SolidColorBrush(Color.FromRgb(255, 77, 77));
                GpuVendorLogo.Data = Geometry.Parse(AmdLogoPath);
                GpuTelemetryCard.BorderBrush = new SolidColorBrush(Color.FromRgb(48, 25, 28));
            }
            else if (gpuBrand == GpuBrand.Intel)
            {
                _gpuPrimaryBrush = new SolidColorBrush(Color.FromRgb(61, 184, 245));
                GpuVendorLogo.Data = Geometry.Parse(IntelLogoPath);
                GpuTelemetryCard.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 38, 56));
            }
            else
            {
                _gpuPrimaryBrush = new SolidColorBrush(Color.FromRgb(61, 214, 140));
                GpuVendorLogo.Data = Geometry.Parse(NvidiaLogoPath);
                GpuTelemetryCard.BorderBrush = new SolidColorBrush(Color.FromRgb(22, 44, 34));
            }

            GpuVendorLogo.Fill = _gpuPrimaryBrush;
            GpuFanBlades.Fill = _gpuPrimaryBrush;
            RpmGpuText.Foreground = _gpuPrimaryBrush;
            if (!string.IsNullOrEmpty(gpuModel) && gpuModel != "GPU")
            {
                GpuSubtitleText.Text = gpuModel;
            }

            if (Fan.HasThirdFan)
            {
                ColGpu2.Width = new GridLength(1, GridUnitType.Star);
                GpuTelemetryCard.Margin = new Thickness(8, 0, 8, 0);
                Gpu2TelemetryCard.Visibility = Visibility.Visible;
                Gpu2VendorLogo.Data = GpuVendorLogo.Data;
                Gpu2VendorLogo.Fill = _gpuPrimaryBrush;
                Gpu2FanBlades.Fill = _gpuPrimaryBrush;
                Gpu2TelemetryCard.BorderBrush = GpuTelemetryCard.BorderBrush;
                RpmGpu2Text.Foreground = _gpuPrimaryBrush;
                if (!string.IsNullOrEmpty(gpuModel) && gpuModel != "GPU")
                {
                    Gpu2SubtitleText.Text = gpuModel;
                }
            }
            else
            {
                ColGpu2.Width = new GridLength(0);
                GpuTelemetryCard.Margin = new Thickness(8, 0, 0, 0);
                Gpu2TelemetryCard.Visibility = Visibility.Collapsed;
            }
        }

        private void rpmTimer_Tick(object sender, EventArgs e)
        {
            int rpm_CPU = 0;
            int percent_CPU = 0;
            int remote_CPU = 0;
            int rpm_GPU = 0;
            int percent_GPU = 0;
            int remote_GPU = 0;
            int rpm_GPU2 = 0;
            int percent_GPU2 = 0;
            int remote_GPU2 = 0;

            if (Fan.HasThirdFan)
            {
                Fan.Read_FanSpeed(ref rpm_CPU, ref percent_CPU, ref remote_CPU,
                                  ref rpm_GPU, ref percent_GPU, ref remote_GPU,
                                  ref rpm_GPU2, ref percent_GPU2, ref remote_GPU2);

                RpmGpu2Text.Text = string.Format("{0} RPM   {1}%   {2}°C",
                    rpm_GPU2 > 0 ? rpm_GPU2 : 0,
                    percent_GPU2 > 0 ? percent_GPU2 : 0,
                    remote_GPU2 > 0 ? remote_GPU2 : 0);

                RpmGpu2Text.Foreground = remote_GPU2 > 85 ? colorRed : (remote_GPU2 > 70 ? colorYellow : _gpuPrimaryBrush);
            }
            else
            {
                Fan.Read_FanSpeed(ref rpm_CPU, ref percent_CPU, ref remote_CPU, ref rpm_GPU, ref percent_GPU, ref remote_GPU);
            }

            RpmCpuText.Text = string.Format("{0} RPM   {1}%   {2}°C",
                rpm_CPU > 0 ? rpm_CPU : 0,
                percent_CPU > 0 ? percent_CPU : 0,
                remote_CPU > 0 ? remote_CPU : 0);

            RpmGpuText.Text = string.Format("{0} RPM   {1}%   {2}°C",
                rpm_GPU > 0 ? rpm_GPU : 0,
                percent_GPU > 0 ? percent_GPU : 0,
                remote_GPU > 0 ? remote_GPU : 0);

            RpmCpuText.Foreground = remote_CPU > 85 ? colorRed : (remote_CPU > 70 ? colorYellow : _cpuPrimaryBrush);
            RpmGpuText.Foreground = remote_GPU > 85 ? colorRed : (remote_GPU > 70 ? colorYellow : _gpuPrimaryBrush);
        }

        private void AutoModeBtn_Click(object sender, RoutedEventArgs e)
        {
            Fan.SetMode(Fan.Mode.Auto);
            SetActiveModeUI(Fan.Mode.Auto);
            OsdOverlay.ShowFan(Fan.Mode.Auto);
        }

        private void MaxModeBtn_Click(object sender, RoutedEventArgs e)
        {
            Fan.SetMode(Fan.Mode.Max);
            SetActiveModeUI(Fan.Mode.Max);
            OsdOverlay.ShowFan(Fan.Mode.Max);
        }

        private void AntiDustModeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Fan.IsAntiDustSupported()) return;
            Fan.AntiDust();
            SetButtonActive(AntiDustModeBtn, true);
            SetActiveModeUI(Fan.Mode.Unknown, AntiDustDesc);
            OsdOverlay.ShowFanAntiDust();
        }

        private void MaxQModeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Fan.IsMaxQSupported()) return;
            Fan.SetMode(Fan.Mode.MaxQ);
            SetActiveModeUI(Fan.Mode.MaxQ);
            OsdOverlay.ShowFan(Fan.Mode.MaxQ);
        }

        private void CustomModeBtn_Click(object sender, RoutedEventArgs e)
        {
            Fan.SetMode(Fan.Mode.Custom);
            SetActiveModeUI(Fan.Mode.Custom);
            OsdOverlay.ShowFan(Fan.Mode.Custom);
        }

        private void OffsetSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            Fan.SetOffset(Convert.ToByte((int)OffsetSlider.Value));
            OffsetValueText.Text = string.Format("{0}%", (int)OffsetSlider.Value);
        }

        private void OffsetSlider_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            OffsetValueText.Text = string.Format("{0}%", (int)OffsetSlider.Value);
        }

        private void OffsetSlider_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Fan.SetOffset(Convert.ToByte((int)OffsetSlider.Value));
            OffsetValueText.Text = string.Format("{0}%", (int)OffsetSlider.Value);
        }

        private void ResetCustomFan_Click(object sender, RoutedEventArgs e)
        {
            CPU_Temp1.Text = "40";
            CPU_Temp2.Text = "60";
            CPU_Temp3.Text = "80";
            CPU_Temp4.Text = "100";
            CPU_Speed1.Text = "35";
            CPU_Speed2.Text = "50";
            CPU_Speed3.Text = "85";
            CPU_Speed4.Text = "100";

            GPU_Temp1.Text = "40";
            GPU_Temp2.Text = "60";
            GPU_Temp3.Text = "80";
            GPU_Temp4.Text = "100";
            GPU_Speed1.Text = "35";
            GPU_Speed2.Text = "50";
            GPU_Speed3.Text = "85";
            GPU_Speed4.Text = "100";

            ShowCurveStatus("Safe defaults restored. Click 'Apply Curve' to save.", colorYellow, autoReset: false);
        }

        private void ApplyCustomFan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int CPU_T1 = Convert.ToInt32(CPU_Temp1.Text);
                int CPU_T2 = Convert.ToInt32(CPU_Temp2.Text);
                int CPU_T3 = Convert.ToInt32(CPU_Temp3.Text);
                int CPU_T4 = Convert.ToInt32(CPU_Temp4.Text);
                int CPU_D1 = Convert.ToInt32(CPU_Speed1.Text);
                int CPU_D2 = Convert.ToInt32(CPU_Speed2.Text);
                int CPU_D3 = Convert.ToInt32(CPU_Speed3.Text);
                int CPU_D4 = Convert.ToInt32(CPU_Speed4.Text);

                int GPU_T1 = Convert.ToInt32(GPU_Temp1.Text);
                int GPU_T2 = Convert.ToInt32(GPU_Temp2.Text);
                int GPU_T3 = Convert.ToInt32(GPU_Temp3.Text);
                int GPU_T4 = Convert.ToInt32(GPU_Temp4.Text);
                int GPU_D1 = Convert.ToInt32(GPU_Speed1.Text);
                int GPU_D2 = Convert.ToInt32(GPU_Speed2.Text);
                int GPU_D3 = Convert.ToInt32(GPU_Speed3.Text);
                int GPU_D4 = Convert.ToInt32(GPU_Speed4.Text);

                // Validate CPU temperatures: must strictly increase within 20°C to 105°C
                if (CPU_T1 < 20 || CPU_T1 >= CPU_T2 || CPU_T2 >= CPU_T3 || CPU_T3 >= CPU_T4 || CPU_T4 > 105)
                {
                    MessageBox.Show("CPU Temperature points must satisfy: 20°C <= Point 1 < Point 2 < Point 3 < Point 4 <= 105°C.",
                        "Invalid CPU Temperature", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate CPU speeds: must be non-decreasing percentages within 0% to 100%
                if (CPU_D1 < 0 || CPU_D1 > CPU_D2 || CPU_D2 > CPU_D3 || CPU_D3 > CPU_D4 || CPU_D4 > 100)
                {
                    MessageBox.Show("CPU Fan Speed points must satisfy: 0% <= Point 1 <= Point 2 <= Point 3 <= Point 4 <= 100%.",
                        "Invalid CPU Fan Speed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate GPU temperatures: must strictly increase within 20°C to 105°C
                if (GPU_T1 < 20 || GPU_T1 >= GPU_T2 || GPU_T2 >= GPU_T3 || GPU_T3 >= GPU_T4 || GPU_T4 > 105)
                {
                    MessageBox.Show("GPU Temperature points must satisfy: 20°C <= Point 1 < Point 2 < Point 3 < Point 4 <= 105°C.",
                        "Invalid GPU Temperature", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate GPU speeds: must be non-decreasing percentages within 0% to 100%
                if (GPU_D1 < 0 || GPU_D1 > GPU_D2 || GPU_D2 > GPU_D3 || GPU_D3 > GPU_D4 || GPU_D4 > 100)
                {
                    MessageBox.Show("GPU Fan Speed points must satisfy: 0% <= Point 1 <= Point 2 <= Point 3 <= Point 4 <= 100%.",
                        "Invalid GPU Fan Speed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Fan.SetCustomFanTable(
                    new FanInfo(CPU_T1, CPU_D1, CPU_T2, CPU_D2, CPU_T3, CPU_D3, CPU_T4, CPU_D4),
                    new FanInfo(GPU_T1, GPU_D1, GPU_T2, GPU_D2, GPU_T3, GPU_D3, GPU_T4, GPU_D4));

                Fan.SetMode(Fan.Mode.Custom);
                SetActiveModeUI(Fan.Mode.Custom);
                OsdOverlay.ShowFan(Fan.Mode.Custom);
                ShowCurveStatus("Custom curve successfully applied to hardware controller.", colorBlue, autoReset: true);
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter valid numerical values for all temperature and speed points.",
                    "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowCurveStatus(string message, Brush brush, bool autoReset = true)
        {
            CurveStatusMessage.Text = message;
            CurveStatusMessage.Foreground = brush;

            _curveStatusResetTimer.Stop();
            if (autoReset)
            {
                _curveStatusResetTimer.Start();
            }
        }
    }
}
