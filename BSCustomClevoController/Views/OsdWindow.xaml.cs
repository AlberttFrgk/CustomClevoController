using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BSCustomClevoController.Views
{
    public partial class OsdWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOPMOST = 0x00000008;

        private readonly DispatcherTimer _hideTimer;
        private int _displaySequence = 0;

        public OsdWindow()
        {
            InitializeComponent();

            _hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1500)
            };
            _hideTimer.Tick += HideTimer_Tick;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT | WS_EX_TOPMOST);
        }

        public void Display(string imageName, string text)
        {
            _hideTimer.Stop();
            _displaySequence++;
            this.BeginAnimation(UIElement.OpacityProperty, null);

            OsdText.Text = text;

            try
            {
                OsdIcon.Source = new BitmapImage(new Uri("pack://application:,,,/BSCustomClevoController;component/Assets/OSD/" + imageName, UriKind.Absolute));
            }
            catch
            {
                try
                {
                    OsdIcon.Source = new BitmapImage(new Uri("/Assets/OSD/" + imageName, UriKind.Relative));
                }
                catch
                {
                    try
                    {
                        string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "OSD", imageName);
                        if (File.Exists(localPath))
                        {
                            OsdIcon.Source = new BitmapImage(new Uri(localPath, UriKind.Absolute));
                        }
                    }
                    catch
                    {
                    }
                }
            }

            // Position at bottom center, exactly matching native Clevo FnKey OSD
            Rect workArea = SystemParameters.WorkArea;
            this.Left = workArea.Left + (workArea.Width - this.Width) / 2;

            if (workArea.Bottom >= SystemParameters.PrimaryScreenHeight - 2)
            {
                // Taskbar is hidden or on another display edge: place 54px above the bottom
                this.Top = SystemParameters.PrimaryScreenHeight - this.Height - 54;
            }
            else
            {
                // Taskbar is docked at bottom: place just above the taskbar
                this.Top = workArea.Bottom - this.Height - 8;
            }

            if (this.Visibility != Visibility.Visible)
            {
                this.Opacity = 0;
                this.Show();
            }

            DoubleAnimation fadeIn = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(100),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            this.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            _hideTimer.Start();
        }

        private void HideTimer_Tick(object sender, EventArgs e)
        {
            _hideTimer.Stop();
            int currentSequence = _displaySequence;

            DoubleAnimation fadeOut = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += (s, args) =>
            {
                if (_displaySequence == currentSequence)
                {
                    this.Hide();
                }
            };

            this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}
