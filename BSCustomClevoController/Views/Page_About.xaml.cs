using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using BSCustomClevoController.Utility;

namespace BSCustomClevoController.Views
{
    public partial class Page_About : Page
    {
        public Page_About()
        {
            InitializeComponent();
        }

        private void OpenUpstreamRepo_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/farukborann/custom-clevo-laptop-controller-windows-app");
        }

        private void OpenCurrentRepo_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/AlberttFrgk/CustomClevoController");
        }

        private void OpenVaioCenter_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/GuilhermeBHanzen/vaio-center");
        }

        private void OpenInsydeKeyboardLight_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/FFeng123/InsydeKeyboardLight");
        }

        private void OpenCppKeyboardColour_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/DeviceIoControl/CppKeyboardColour");
        }

        private void OpenTuxedoKeyboard_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/tuxedocomputers/tuxedo-keyboard");
        }

        private void OpenUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri parsedUri) ||
                !string.Equals(parsedUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(parsedUri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationManager.Format("CommonErrorFmt", ex.Message), LocalizationManager.T("CommonNotice"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
