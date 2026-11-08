using BSCustomClevoController.Utility;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BSCustomClevoController.Controls;

namespace BSCustomClevoController.Views
{
    public partial class Page_PerformanceSettings : Page
    {
        private readonly SolidColorBrush colorAccent = new SolidColorBrush(Color.FromRgb(0, 120, 215)); // #0078D7
        private readonly SolidColorBrush colorBadgeBg = new SolidColorBrush(Color.FromRgb(27, 54, 93));  // #1B365D
        private readonly SolidColorBrush colorCardInactiveBorder = new SolidColorBrush(Color.FromRgb(60, 60, 70)); // #3C3C46
        private readonly SolidColorBrush colorCardActiveBorder = new SolidColorBrush(Color.FromRgb(0, 120, 215));   // #0078D7
        private readonly SolidColorBrush colorCardActiveBg = new SolidColorBrush(Color.FromRgb(36, 48, 64));       // subtle cobalt tint
        private readonly SolidColorBrush colorCardDefaultBg = new SolidColorBrush(Color.FromRgb(42, 42, 48));      // #2A2A30
        private readonly SolidColorBrush colorWhite = new SolidColorBrush(Color.FromRgb(255, 255, 255));           // #FFFFFF

        private readonly string QuietModeDes = "Fans run at minimum speed to keep acoustics quiet. Recommended for reading, typing, and quiet environments.";
        private readonly string PowerSaveModeDes = "Processor speed and power consumption are reduced to maximize battery runtime.";
        private readonly string EntertainmentModeDes = "Balanced performance and fan noise for everyday multitasking, media, and gaming.";
        private readonly string PerformanceModeDes = "Maximum hardware power and proactive cooling for heavy workloads and gaming.";

        public Page_PerformanceSettings()
        {
            InitializeComponent();
            ApplyModeUI(PowerMode.GetStatus());
            InitializeHardwarePolicies();
        }

        private void InitializeHardwarePolicies()
        {
            // Energy Star 1.5C
            bool energyStarSupported = Capabilities.IsSupported("EnergyStar15C");
            EnergyStarSwitch.IsEnabled = energyStarSupported;
            EnergyStarCard.Opacity = energyStarSupported ? 1.0 : 0.55;

            // Dynamic Tuning (PCIe ASPM)
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
                    modeDetais.Text = QuietModeDes;
                    break;
                case PowerMode.Mode.Powersaving:
                    modeDetais.Text = PowerSaveModeDes;
                    break;
                case PowerMode.Mode.Performance:
                    modeDetais.Text = PerformanceModeDes;
                    break;
                case PowerMode.Mode.Entertainment:
                    modeDetais.Text = EntertainmentModeDes;
                    break;
            }

            (Application.Current.MainWindow as MainWindow)?.UpdateTrayTooltip(mode.ToString());
        }

        private void SetCardState(Button cardBtn, Border badge, TextBlock badgeText, TextBlock titleText, bool isSelected)
        {
            cardBtn.BorderBrush = isSelected ? colorCardActiveBorder : colorCardInactiveBorder;
            cardBtn.Background = isSelected ? colorCardActiveBg : colorCardDefaultBg;
            badge.Visibility = isSelected ? Visibility.Visible : Visibility.Hidden;
            badge.Background = colorBadgeBg;
            badgeText.Text = isSelected ? "Active" : string.Empty;
            badgeText.Foreground = colorAccent;
            titleText.Foreground = isSelected ? colorAccent : colorWhite;
        }

        private void QuietModeBtn_Click(object sender, RoutedEventArgs e)
        {
            PowerMode.SetPowerMode(PowerMode.Mode.Quiet);
            ApplyModeUI(PowerMode.Mode.Quiet);
            OsdOverlay.ShowPowerMode(PowerMode.Mode.Quiet);
        }

        private void PowerSaveModeBtn_Click(object sender, RoutedEventArgs e)
        {
            PowerMode.SetPowerMode(PowerMode.Mode.Powersaving);
            ApplyModeUI(PowerMode.Mode.Powersaving);
            OsdOverlay.ShowPowerMode(PowerMode.Mode.Powersaving);
        }

        private void EntertainmentModeBtn_Click(object sender, RoutedEventArgs e)
        {
            PowerMode.SetPowerMode(PowerMode.Mode.Entertainment);
            ApplyModeUI(PowerMode.Mode.Entertainment);
            OsdOverlay.ShowPowerMode(PowerMode.Mode.Entertainment);
        }

        private void PerformanceModeBtn_Click(object sender, RoutedEventArgs e)
        {
            PowerMode.SetPowerMode(PowerMode.Mode.Performance);
            ApplyModeUI(PowerMode.Mode.Performance);
            OsdOverlay.ShowPowerMode(PowerMode.Mode.Performance);
        }

        private void QuietModeBtn_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            modeDetais.Text = QuietModeDes;
        }

        private void PowerSaveModeBtn_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            modeDetais.Text = PowerSaveModeDes;
        }

        private void EntertainmentModeBtn_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            modeDetais.Text = EntertainmentModeDes;
        }

        private void PerformanceModeBtn_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            modeDetais.Text = PerformanceModeDes;
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
