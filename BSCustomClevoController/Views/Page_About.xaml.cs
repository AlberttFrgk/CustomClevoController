    using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

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
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open browser: " + ex.Message, "Open Link", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
