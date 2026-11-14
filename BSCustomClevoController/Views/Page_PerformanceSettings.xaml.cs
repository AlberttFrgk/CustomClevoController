using BSCustomClevoController.Utility;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BSCustomClevoController.Controls;

namespace BSCustomClevoController.Views
{
    public partial class Page_PerformanceSettings : Page
    {
        private readonly SolidColorBrush _colorAccent = new SolidColorBrush(Color.FromRgb(0, 120, 215));
        private readonly SolidColorBrush _colorBadgeBg = new SolidColorBrush(Color.FromRgb(27, 54, 93));
        private readonly SolidColorBrush _colorCardInactiveBorder = new SolidColorBrush(Color.FromRgb(60, 60, 70));
        private readonly SolidColorBrush _colorCardActiveBorder = new SolidColorBrush(Color.FromRgb(0, 120, 215));
        private readonly SolidColorBrush _colorCardActiveBg = new SolidColorBrush(Color.FromRgb(36, 48, 64));
        private readonly SolidColorBrush _colorCardDefaultBg = new SolidColorBrush(Color.FromRgb(42, 42, 48));
        private readonly SolidColorBrush _colorWhite = new SolidColorBrush(Color.FromRgb(255, 255, 255));

        private static string QuietModeDesc => LocalizationManager.T("PerfQuietDetail");
        private static string PowerSaveModeDesc => LocalizationManager.T("PerfPowerSaverDetail");
        private static string EntertainmentModeDesc => LocalizationManager.T("PerfEntertainmentDetail");
        private static string PerformanceModeDesc => LocalizationManager.T("PerfPerformanceDetail");

        public Page_PerformanceSettings()
        {
            InitializeComponent();
            ApplyModeUI(PowerMode.GetStatus());
            InitializeHardwarePolicies();
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            ApplyModeUI(PowerMode.GetStatus());
        }

        private void InitializeHardwarePolicies()
        {
            bool energyStarSupported = Capabilities.IsSupported("EnergyStar15C");
            EnergyStarSwitch.IsEnabled = energyStarSupported;
            EnergyStarCard.Opacity = energyStarSupported ? 1.0 : 0.55;

            bool dynamicTuningSupported = Capabilities.IsSupported("DynamicTuning");
            DynamicTuningSwitch.IsEnabled = dynamicTuningSupported;
            DynamicTuningCard.Opacity = dynamicTuningSupported ? 1.0 : 0.55;
        }

        public void ApplyModeUI(PowerMode.Mode mode)
        {
            SetCardState(QuietModeBtn, QuietActiveBadge, QuietActiveText, QuietModeBtnText, mode == PowerMode.Mode.Quiet);
            SetCardState(PowerSaveModeBtn, PowerSaveActiveBadge, PowerSaveActiveText, PowerSaveModeBtnText, mode == PowerMode.Mode.Powersaving);
            SetCardState(EntertainmentModeBtn, EntertainmentActiveBadge, EntertainmentActiveText, EntertainmentModeBtnText, mode == PowerMode.Mode.Entertainment);
            SetCardState(PerformanceModeBtn, PerformanceActiveBadge, PerformanceActiveText, PerformanceModeBtnText, mode == PowerMode.Mode.Performance);

            switch (mode)
            {
                case PowerMode.Mode.Quiet:
                    modeDetais.Text = QuietModeDesc;
                    break;
                case PowerMode.Mode.Powersaving:
                    modeDetais.Text = PowerSaveModeDesc;
                    break;
                case PowerMode.Mode.Performance:
                    modeDetais.Text = PerformanceModeDesc;
                    break;
                case PowerMode.Mode.Entertainment:
                    modeDetais.Text = EntertainmentModeDesc;
                    break;
            }

            (Application.Current.MainWindow as MainWindow)?.UpdateTrayTooltip(mode.ToString());
        }

        private void SetCardState(Button cardBtn, Border badge, TextBlock badgeText, TextBlock titleText, bool isSelected)
        {
            cardBtn.BorderBrush = isSelected ? _colorCardActiveBorder : _colorCardInactiveBorder;
            cardBtn.Background = isSelected ? _colorCardActiveBg : _colorCardDefaultBg;
            badge.Visibility = isSelected ? Visibility.Visible : Visibility.Hidden;
            badge.Background = _colorBadgeBg;
            badgeText.Text = isSelected ? LocalizationManager.T("PerfActive") : string.Empty;
            badgeText.Foreground = _colorAccent;
            titleText.Foreground = isSelected ? _colorAccent : _colorWhite;
        }

        private void SelectPowerMode(PowerMode.Mode mode)
        {
            PowerMode.SetPowerMode(mode);
            ApplyModeUI(mode);
            OsdOverlay.ShowPowerMode(mode);
        }

        private void QuietModeBtn_Click(object sender, RoutedEventArgs e) => SelectPowerMode(PowerMode.Mode.Quiet);
        private void PowerSaveModeBtn_Click(object sender, RoutedEventArgs e) => SelectPowerMode(PowerMode.Mode.Powersaving);
        private void EntertainmentModeBtn_Click(object sender, RoutedEventArgs e) => SelectPowerMode(PowerMode.Mode.Entertainment);
        private void PerformanceModeBtn_Click(object sender, RoutedEventArgs e) => SelectPowerMode(PowerMode.Mode.Performance);

        private void QuietModeBtn_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            modeDetais.Text = QuietModeDesc;
        }

        private void PowerSaveModeBtn_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            modeDetais.Text = PowerSaveModeDesc;
        }

        private void EntertainmentModeBtn_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            modeDetais.Text = EntertainmentModeDesc;
        }

        private void PerformanceModeBtn_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            modeDetais.Text = PerformanceModeDesc;
        }

        private void EnergyStarSwitch_Click(object sender, RoutedEventArgs e)
        {
            Capabilities.SetEnergyStar15C(EnergyStarSwitch.IsChecked == true);
        }

        private void DynamicTuningSwitch_Click(object sender, RoutedEventArgs e)
        {
            Capabilities.SetDynamicTuning(DynamicTuningSwitch.IsChecked == true);
        }
    }
}
