using BSCustomClevoController.Utility;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static BSCustomClevoController.Utility.Structs;
using BSCustomClevoController.Controls;


namespace BSCustomClevoController.Views
{
    public partial class Page_Settings : Page
    {
        public Page_Settings()
        {
            InitializeComponent();
            LoadStartupState();
            LoadOsdState();
            LoadDriverState();
            LoadCapabilities();
        }

        private void LoadDriverState()
        {
            try
            {
                bool working = AcpiDriverInstaller.IsDriverWorking();
                if (working)
                {
                    DriverStatusBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x2D, 0x42));
                    DriverStatusBadge.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x25, 0x63, 0xEB));
                    DriverStatusText.Text = "Installed";
                    DriverStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x79, 0xCF, 0xFF));
                    ReinstallDriverBtn.Content = "Reinstall";
                    UninstallDriverBtn.IsEnabled = true;
                }
                else
                {
                    DriverStatusBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x1B, 0x1B));
                    DriverStatusBadge.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xDC, 0x26, 0x26));
                    DriverStatusText.Text = "Not Installed";
                    DriverStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFA, 0x79, 0x70));
                    ReinstallDriverBtn.Content = "Install Driver";
                    UninstallDriverBtn.IsEnabled = false;
                }
            }
            catch
            {
            }
        }

        private async void ReinstallDriver_Click(object sender, RoutedEventArgs e)
        {
            bool isWorking = AcpiDriverInstaller.IsDriverWorking();
            string actionText = isWorking ? "reinstall" : "install";

            var confirm = MessageBox.Show(
                $"Are you sure you want to {actionText} the AcpiBridge driver? A system restart will be required to complete setup.",
                isWorking ? "Reinstall Driver" : "Install Driver",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            ReinstallDriverBtn.IsEnabled = false;
            UninstallDriverBtn.IsEnabled = false;
            DriverStatusText.Text = "Installing...";
            DriverStatusDesc.Text = "Adding AcpiBridge driver package and scanning PnP devices...";

            bool rebootRequired = false;
            bool success = await Task.Run(() => AcpiDriverInstaller.InstallDriver(out rebootRequired, force: true));

            ReinstallDriverBtn.IsEnabled = true;
            DriverStatusDesc.Text = "Hardware driver interface for fan speeds, power modes, battery limits, and hotkeys.";
            LoadDriverState();

            if (success)
            {
                StartupCheckBox.IsChecked = StartupManager.IsRunAtStartupEnabled();
                Capabilities.InvalidateCache();
                LoadCapabilities();

                var res = MessageBox.Show(
                    "AcpiBridge driver installed successfully. A restart is required to complete setup.\n\nWould you like to restart your PC now?",
                    "Restart Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    DriverStatusText.Text = "Restarting...";
                    AcpiDriverInstaller.RestartComputer();
                    await Task.Delay(1000);
                    Application.Current.Shutdown();
                }
            }
            else
            {
                MessageBox.Show(
                    "Failed to install or verify AcpiBridge driver. Please ensure driver files exist in the AcpiBridge folder.",
                    "Driver Installation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void UninstallDriver_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show(
                "Are you sure you want to uninstall the AcpiBridge driver? Hardware controls will stop functioning, and a system restart will be required.",
                "Uninstall Driver",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes) return;

            ReinstallDriverBtn.IsEnabled = false;
            UninstallDriverBtn.IsEnabled = false;
            DriverStatusText.Text = "Removing...";
            DriverStatusDesc.Text = "Removing AcpiBridge service and deleting driver package...";

            bool success = await Task.Run(() => AcpiDriverInstaller.UninstallDriver());

            ReinstallDriverBtn.IsEnabled = true;
            DriverStatusDesc.Text = "Hardware driver interface for fan speeds, power modes, battery limits, and hotkeys.";

            if (success)
            {
                StartupManager.SetRunAtStartup(false);
                StartupCheckBox.IsChecked = false;
                Capabilities.InvalidateCache();
                try
                {
                    Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\BSCustomClevoController", true)?.DeleteValue("DefaultsApplied", false);
                }
                catch
                {
                }
                DriverStatusBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x2E, 0x1B));
                DriverStatusBadge.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD9, 0x77, 0x06));
                DriverStatusText.Text = "Pending Restart";
                DriverStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFB, 0xBF, 0x24));
                ReinstallDriverBtn.Content = "Install Driver";
                UninstallDriverBtn.IsEnabled = false;

                var restartRes = MessageBox.Show(
                    "AcpiBridge driver uninstalled successfully. A restart is required to complete driver removal.\n\nWould you like to restart your PC now?",
                    "Restart Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (restartRes == MessageBoxResult.Yes)
                {
                    DriverStatusText.Text = "Restarting...";
                    AcpiDriverInstaller.RestartComputer();
                    await Task.Delay(1000);
                    Application.Current.Shutdown();
                }
            }
            else
            {
                LoadDriverState();
                MessageBox.Show(
                    "Failed to uninstall AcpiBridge driver. Administrator privileges may be required.",
                    "Driver Uninstallation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadStartupState()
        {
            try
            {
                StartupCheckBox.IsChecked = StartupManager.IsRunAtStartupEnabled();
            }
            catch
            {
                StartupManager.SetRunAtStartup(false);
                StartupCheckBox.IsChecked = false;
            }
        }

        private void LoadOsdState()
        {
            try
            {
                OsdCheckBox.IsChecked = OsdOverlay.IsEnabled;
            }
            catch
            {
                OsdCheckBox.IsChecked = true;
            }
        }

        private void OsdCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool enabled = OsdCheckBox.IsChecked == true;
            OsdOverlay.IsEnabled = enabled;
            SystemPreferences.UpdateHookState();
        }

        private void StartupCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool wantEnable = StartupCheckBox.IsChecked == true;
            try
            {
                bool success = StartupManager.SetRunAtStartup(wantEnable);
                StartupCheckBox.IsChecked = StartupManager.IsRunAtStartupEnabled();
                if (!success)
                {
                    MessageBox.Show(
                        "Unable to configure Windows startup. Please make sure the app has administrator privileges.",
                        "Startup Configuration",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StartupCheckBox.IsChecked = StartupManager.IsRunAtStartupEnabled();
                MessageBox.Show(
                    "Error updating startup settings: " + ex.Message,
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ResetFactory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to restore all fan, power mode, and keyboard lighting settings to factory defaults?",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                SystemPreferences.ApplyFactoryDefaults();

                (Application.Current.MainWindow as MainWindow)?.ReloadCachedPages();

                MessageBox.Show(
                    "Factory default settings have been successfully restored.",
                    "Settings Reset",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error resetting to factory defaults: " + ex.Message,
                    "Reset Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mw)
            {
                mw.ExitApplication();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void LoadCapabilities()
        {
            try
            {
                FeaturesListBox.ItemsSource = Capabilities.Check();
            }
            catch (Exception ex)
            {
                FeaturesListBox.ItemsSource = new System.Collections.Generic.List<CapabilityItem>
                {
                    new CapabilityItem
                    {
                        Name = "Diagnostic query error: " + ex.Message,
                        IsSupported = false,
                        RawKey = "Error"
                    }
                };
            }
        }

        private void RefreshCapabilities_Click(object sender, RoutedEventArgs e)
        {
            LoadCapabilities();
        }

        private async void CopyCapabilities_Click(object sender, RoutedEventArgs e)
        {
            bool copied = CopyAllToClipboard();
            if (copied && CopyCapabilitiesBtn != null)
            {
                string original = CopyCapabilitiesBtn.Content.ToString();
                CopyCapabilitiesBtn.Content = "Copied!";
                await Task.Delay(1500);
                CopyCapabilitiesBtn.Content = original;
            }
        }

        private void CopyAll_Click(object sender, RoutedEventArgs e)
        {
            CopyAllToClipboard();
        }

        private static bool SafeSetClipboardText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            // 1. Try WinForms Clipboard with built-in retry parameter (10 retries, 50ms interval)
            try
            {
                System.Windows.Forms.Clipboard.SetDataObject(text, true, 10, 50);
                return true;
            }
            catch
            {
            }

            // 2. Try WPF Clipboard with retry loop (10 retries, 40ms interval)
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Clipboard.SetDataObject(text, true);
                    return true;
                }
                catch (COMException)
                {
                    Thread.Sleep(40);
                }
                catch
                {
                    Thread.Sleep(40);
                }
            }

            return false;
        }

        private bool CopyAllToClipboard()
        {
            if (FeaturesListBox.ItemsSource == null) return false;

            var sb = new StringBuilder();
            sb.AppendLine("=== Hardware EC Capabilities Diagnostic ===");
            sb.AppendLine("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();

            foreach (var item in FeaturesListBox.ItemsSource)
            {
                if (item != null)
                {
                    sb.AppendLine(item.ToString());
                }
            }

            bool success = SafeSetClipboardText(sb.ToString());
            if (!success)
            {
                MessageBox.Show("Could not copy to clipboard. Another application is currently holding the clipboard lock.", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return success;
        }
    }
}
