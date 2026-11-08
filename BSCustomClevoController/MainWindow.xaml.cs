using MahApps.Metro.Controls;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Navigation;
using BSCustomClevoController.Controls;

namespace BSCustomClevoController
{
    public partial class MainWindow : MetroWindow
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ChangeWindowMessageFilter(int message, int dwFlag);

        private const int MSGFLT_ADD = 1;

        private const int SW_RESTORE = 9;

        private int _clevoMsg;
        private int _clevoWmiMsg;
        private bool _startMinimized;
        private DateTime _lastTouchpadToggleTime = DateTime.MinValue;
        private readonly Frame _frame;
        private NotifyIcon _notifyIcon;
        private readonly System.Collections.Generic.Dictionary<string, Page> _pageCache =
            new System.Collections.Generic.Dictionary<string, Page>(StringComparer.OrdinalIgnoreCase);

        public MainWindow()
        {
            InitializeComponent();

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BSCustomClevoController.Assets.main_icon.ico");
            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon(stream),
                Text = "Custom Clevo Controller",
                Visible = true
            };

            // Apply stock defaults for first-time users or restore saved hardware settings on boot
            Utility.SystemPreferences.InitializeHardwareOnStartup();

            UpdateTrayTooltip(Controls.PowerMode.GetStatus().ToString());

            Utility.SystemPreferences.Initialize();

            Utility.StartupManager.EnsureConfiguredCorrectly();

            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();

            var perfProfileMenu = new ToolStripMenuItem("Performance Profile");
            var quietItem = new ToolStripMenuItem("Quiet", null, (s, e) => ApplyPowerProfile(Controls.PowerMode.Mode.Quiet));
            var powerSaveItem = new ToolStripMenuItem("Power Saver", null, (s, e) => ApplyPowerProfile(Controls.PowerMode.Mode.Powersaving));
            var entertainmentItem = new ToolStripMenuItem("Entertainment", null, (s, e) => ApplyPowerProfile(Controls.PowerMode.Mode.Entertainment));
            var performanceItem = new ToolStripMenuItem("Performance", null, (s, e) => ApplyPowerProfile(Controls.PowerMode.Mode.Performance));
            perfProfileMenu.DropDownItems.Add(quietItem);
            perfProfileMenu.DropDownItems.Add(powerSaveItem);
            perfProfileMenu.DropDownItems.Add(entertainmentItem);
            perfProfileMenu.DropDownItems.Add(performanceItem);
            _notifyIcon.ContextMenuStrip.Items.Add(perfProfileMenu);

            var fanProfileMenu = new ToolStripMenuItem("Fan Profile");
            var autoFanItem = new ToolStripMenuItem("Automatic", null, (s, e) => ApplyFanProfile(Controls.Fan.Mode.Auto));
            var maxFanItem = new ToolStripMenuItem("Max Speed", null, (s, e) => ApplyFanProfile(Controls.Fan.Mode.Max));
            var customFanItem = new ToolStripMenuItem("Custom Curve", null, (s, e) => ApplyFanProfile(Controls.Fan.Mode.Custom));
            fanProfileMenu.DropDownItems.Add(autoFanItem);
            fanProfileMenu.DropDownItems.Add(maxFanItem);
            fanProfileMenu.DropDownItems.Add(customFanItem);
            _notifyIcon.ContextMenuStrip.Items.Add(fanProfileMenu);

            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());

            _notifyIcon.ContextMenuStrip.Opening += (s, e) =>
            {
                try
                {
                    var currentPower = Controls.PowerMode.GetStatus();
                    quietItem.Checked = (currentPower == Controls.PowerMode.Mode.Quiet);
                    powerSaveItem.Checked = (currentPower == Controls.PowerMode.Mode.Powersaving);
                    entertainmentItem.Checked = (currentPower == Controls.PowerMode.Mode.Entertainment);
                    performanceItem.Checked = (currentPower == Controls.PowerMode.Mode.Performance);

                    var currentFan = Controls.Fan.GetStatus();
                    autoFanItem.Checked = (currentFan.Mode == Controls.Fan.Mode.Auto);
                    maxFanItem.Checked = (currentFan.Mode == Controls.Fan.Mode.Max);
                    customFanItem.Checked = (currentFan.Mode == Controls.Fan.Mode.Custom);
                }
                catch
                {
                }
            };

            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Keyboard Lighting", null, notifyIconContext_KeyboardMenuClick));
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Battery", null, notifyIconContext_BatteryMenuClick));
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("System Tools", null, notifyIconContext_OtherMenuClick));
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Settings", null, notifyIconContext_SettingsMenuClick));
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("About", null, notifyIconContext_AboutMenuClick));
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, notifyIconContext_ExitClick));

            _notifyIcon.MouseClick += delegate (object sender, MouseEventArgs args)
            {
                if (args.Button == MouseButtons.Left)
                {
                    BringToForeground();
                }
            };

            _notifyIcon.DoubleClick += delegate (object sender, EventArgs args)
            {
                BringToForeground();
            };

            //Set Hamburger Navigation
            _frame = new Frame { NavigationUIVisibility = NavigationUIVisibility.Hidden };
            _frame.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            _frame.VerticalAlignment = VerticalAlignment.Stretch;
            _frame.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;

            HamburgerMenuControl.Content = _frame;
            _frame.Navigate(GetOrCreatePage("PerformanceSettings"));

            string[] cmdArgs = Environment.GetCommandLineArgs();
            for (int i = 1; i < cmdArgs.Length; i++)
            {
                if (string.Equals(cmdArgs[i], "/minimized", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(cmdArgs[i], "-minimized", StringComparison.OrdinalIgnoreCase))
                {
                    _startMinimized = true;
                    WindowState = WindowState.Minimized;
                    Hide();
                    break;
                }
            }
        }

        public Page GetOrCreatePage(string pageTag)
        {
            if (!_pageCache.TryGetValue(pageTag, out Page page))
            {
                switch (pageTag)
                {
                    case "PerformanceSettings":
                        page = new Views.Page_PerformanceSettings();
                        break;
                    case "FanSettings":
                        page = new Views.Page_FanSettings();
                        break;
                    case "KeyboardSettings":
                        page = new Views.Page_KeyboardSettings();
                        break;
                    case "BatterySettings":
                        page = new Views.Page_BatterySettings();
                        break;
                    case "OtherSettings":
                        page = new Views.Page_OtherSettings();
                        break;
                    case "Settings":
                        page = new Views.Page_Settings();
                        break;
                    case "About":
                        page = new Views.Page_About();
                        break;
                    default:
                        page = new Views.Page_PerformanceSettings();
                        break;
                }
                _pageCache[pageTag] = page;
            }
            return page;
        }

        public void ReloadCachedPages()
        {
            var keys = new System.Collections.Generic.List<string>(_pageCache.Keys);
            foreach (var key in keys)
            {
                if (!string.Equals(key, "OtherSettings", StringComparison.OrdinalIgnoreCase))
                {
                    _pageCache.Remove(key);
                }
            }
        }

        public void UpdateTrayTooltip(string extraInfo = null)
        {
            if (_notifyIcon == null) return;
            try
            {
                string text = string.IsNullOrWhiteSpace(extraInfo)
                    ? "Custom Clevo Controller"
                    : string.Format("Custom Clevo Controller ({0})", extraInfo);

                if (text.Length > 63)
                {
                    text = text.Substring(0, 60) + "...";
                }
                _notifyIcon.Text = text;
            }
            catch
            {
            }
        }

        private void NavigateToPage(int index, string pageTag)
        {
            _startMinimized = false;
            Show();
            WindowState = WindowState.Normal;
            HamburgerMenuControl.SelectedIndex = index;
            _frame.Navigate(GetOrCreatePage(pageTag));
            while (_frame.NavigationService.CanGoBack)
            {
                _frame.NavigationService.RemoveBackEntry();
            }

            var collection = HamburgerMenuControl.ItemsSource as System.Collections.IList;
            if (collection != null && index >= 0 && index < collection.Count)
            {
                UpdateSidebarIcons(collection[index] as HamburgerMenuIconItem);
            }
        }

        private void UpdateSidebarIcons(HamburgerMenuIconItem activeItem)
        {
            if (activeItem == null) return;
            var collection = HamburgerMenuControl.ItemsSource as System.Collections.IEnumerable;
            if (collection == null) return;

            foreach (HamburgerMenuIconItem item in collection)
            {
                if (item == activeItem)
                {
                    item.Icon = "/Assets/Main_Window/" + item.Tag + "_Selected.png";
                }
                else
                {
                    item.Icon = "/Assets/Main_Window/" + item.Tag + ".png";
                }
            }
        }

        private void notifyIconContext_PerformanceMenuClick(object sender, EventArgs e)
        {
            NavigateToPage(0, "PerformanceSettings");
        }

        private void notifyIconContext_FanMenuClick(object sender, EventArgs e)
        {
            NavigateToPage(1, "FanSettings");
        }

        private void notifyIconContext_KeyboardMenuClick(object sender, EventArgs e)
        {
            NavigateToPage(2, "KeyboardSettings");
        }

        private void notifyIconContext_BatteryMenuClick(object sender, EventArgs e)
        {
            NavigateToPage(3, "BatterySettings");
        }

        private void notifyIconContext_OtherMenuClick(object sender, EventArgs e)
        {
            NavigateToPage(4, "OtherSettings");
        }

        private void notifyIconContext_SettingsMenuClick(object sender, EventArgs e)
        {
            NavigateToPage(5, "Settings");
        }

        private void notifyIconContext_AboutMenuClick(object sender, EventArgs e)
        {
            NavigateToPage(6, "About");
        }

        public void SyncPowerLineUI(bool isAcOnline)
        {
            try
            {
                if (_pageCache.TryGetValue("KeyboardSettings", out Page page) && page is Views.Page_KeyboardSettings kbPage)
                {
                    kbPage.UpdateBatterySoftwareAnimationState(isAcOnline);
                }
            }
            catch
            {
            }
        }

        internal void SyncKeyboardSettingsUI()
        {
            try
            {
                if (_pageCache.TryGetValue("KeyboardSettings", out Page page) && page is Views.Page_KeyboardSettings kbPage)
                {
                    kbPage.SyncFromHardwareHotkey();
                }
            }
            catch
            {
            }
        }

        internal void OpenKeyboardSettingsPage()
        {
            try
            {
                BringToForeground();
                NavigateToPage(2, "KeyboardSettings");
            }
            catch
            {
            }
        }

        public void ExitApplication()
        {
            App.CleanupOnExit();
            DisposeNotifyIcon();
            System.Windows.Application.Current.Shutdown();
        }

        public void BringToForeground()
        {
            _startMinimized = false;
            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
            Topmost = true;
            Topmost = false;
            Focus();

            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                ShowWindow(helper.Handle, SW_RESTORE);
                SetForegroundWindow(helper.Handle);
            }
        }

        private void notifyIconContext_ExitClick(object sender, EventArgs e)
        {
            ExitApplication();
        }

        protected override void OnClosed(EventArgs e)
        {
            App.CleanupOnExit();
            DisposeNotifyIcon();
            base.OnClosed(e);
        }

        private void DisposeNotifyIcon()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                Hide();

            base.OnStateChanged(e);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (_startMinimized)
            {
                _startMinimized = false;
                WindowState = WindowState.Minimized;
                Hide();
            }
        }

        private void HamburgerMenuControl_ItemClick(object sender, ItemClickEventArgs args)
        {
            var clicked = (HamburgerMenuIconItem)args.ClickedItem;
            if (clicked?.Tag != null)
            {
                _frame.Navigate(GetOrCreatePage(clicked.Tag.ToString()));
                while (_frame.NavigationService.CanGoBack)
                {
                    _frame.NavigationService.RemoveBackEntry();
                }
                UpdateSidebarIcons(clicked);
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        protected override void OnPreviewMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (System.Windows.Input.Keyboard.FocusedElement is DependencyObject focused)
            {
                System.Windows.Controls.TextBox textBox = focused as System.Windows.Controls.TextBox;
                if (textBox == null)
                {
                    DependencyObject cur = focused;
                    while (cur != null)
                    {
                        if (cur is System.Windows.Controls.TextBox tb)
                        {
                            textBox = tb;
                            break;
                        }
                        cur = System.Windows.Media.VisualTreeHelper.GetParent(cur);
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
                        check = System.Windows.Media.VisualTreeHelper.GetParent(check);
                    }

                    if (!isInside)
                    {
                        Focus();
                    }
                }
            }
        }

        private void ApplyPowerProfile(Controls.PowerMode.Mode mode)
        {
            try
            {
                Controls.PowerMode.SetPowerMode(mode);
                Utility.OsdOverlay.ShowPowerMode(mode);
                SyncPowerProfileUI(mode);
            }
            catch
            {
            }
        }

        internal void SyncPowerProfileUI(Controls.PowerMode.Mode mode)
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateTrayTooltip(mode.ToString());
                    if (_pageCache.TryGetValue("PerformanceSettings", out Page p) && p is Views.Page_PerformanceSettings ps)
                    {
                        ps.ApplyModeUI(mode);
                    }
                }));
            }
            catch
            {
            }
        }

        private void ApplyFanProfile(Controls.Fan.Mode mode)
        {
            try
            {
                Controls.Fan.SetMode(mode);
                Utility.OsdOverlay.ShowFan(mode);
                SyncFanProfileUI(mode);
            }
            catch
            {
            }
        }

        internal void SyncFanProfileUI(Controls.Fan.Mode mode)
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_pageCache.TryGetValue("FanSettings", out Page p) && p is Views.Page_FanSettings fs)
                    {
                        fs.SetActiveModeUI(mode);
                    }
                }));
            }
            catch
            {
            }
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                _clevoMsg = RegisterWindowMessage("ClevoMsg_ControlCenter");
                _clevoWmiMsg = RegisterWindowMessage("ClevoMsg_ControlCenter_WMI");

                if (_clevoMsg != 0)
                {
                    ChangeWindowMessageFilter(_clevoMsg, MSGFLT_ADD);
                }
                if (_clevoWmiMsg != 0)
                {
                    ChangeWindowMessageFilter(_clevoWmiMsg, MSGFLT_ADD);
                }

                var source = PresentationSource.FromVisual(this) as HwndSource;
                if (source != null)
                {
                    source.AddHook(WndProc);
                }
            }
            catch
            {
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0011 || msg == 0x0016) // WM_QUERYENDSESSION or WM_ENDSESSION
            {
                App.CleanupOnExit();
                return (IntPtr)1;
            }

            if (msg == 0x0218) // WM_POWERBROADCAST
            {
                bool ac = System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;
                Controls.KeyboardEffects.HandlePowerStatusChanged(ac);
                SyncPowerLineUI(ac);
            }

            if (msg == _clevoMsg || msg == _clevoWmiMsg)
            {
                int cmd = wParam.ToInt32();
                switch (cmd)
                {
                    case 266: // Fan (Cooler Boost / Max Fan)
                        Controls.Fan.ToggleAutoMaxFan();
                        handled = true;
                        break;
                    case 271: // FN_3
                    case 258: // SituationMode
                        Controls.PowerMode.CyclePowerMode();
                        handled = true;
                        break;
                    case 259: // Touchpad
                        if (DateTime.UtcNow >= Controls.Touchpad.SuppressHardwareEventsUntilUtc &&
                            DateTime.UtcNow >= _lastTouchpadToggleTime.AddMilliseconds(800))
                        {
                            _lastTouchpadToggleTime = DateTime.UtcNow;
                            Controls.Touchpad.ToggleTouchpad();
                        }
                        handled = true;
                        break;
                    case 260: // Camera / CCD
                        Utility.OsdOverlay.ShowCamera(Utility.SystemPreferences.GetCameraState());
                        handled = true;
                        break;
                    case 261: // Airplane
                        Utility.OsdOverlay.ShowAirplane(Utility.SystemPreferences.GetAirplaneModeState());
                        handled = true;
                        break;
                    case 263: // Caps Lock
                        Utility.OsdOverlay.ShowCaps(Utility.SystemPreferences.GetCapsLockState());
                        handled = true;
                        break;
                    case 264: // Scroll Lock
                        Utility.OsdOverlay.ShowScroll(Utility.SystemPreferences.GetScrollLockState());
                        handled = true;
                        break;
                    case 265: // Num Lock
                        Utility.OsdOverlay.ShowNum(Utility.SystemPreferences.GetNumLockState());
                        handled = true;
                        break;
                    case 262: // WinKey / FlexiKey hotkey
                        if (DateTime.UtcNow >= Utility.SystemPreferences.LastWinKeyChangeTime.AddMilliseconds(500))
                        {
                            Utility.SystemPreferences.ToggleWinKeyLock();
                        }
                        handled = true;
                        break;
                    case 267: // Generic hardware change notification (backlight, fan, power)
                        Controls.Fan.CheckAndSyncFanFromHardware();
                        handled = true;
                        break;
                    case 515: // KbBrightness
                    case 516: // WhiteKbBrightness
                    case 518: // PerKbBrightness
                        try
                        {
                            var kbStatus = Controls.KeyboardBackLight.GetStatus();
                            int lvl = (kbStatus.LightState && kbStatus.BackLight.A > 0) ? (int)Math.Round(kbStatus.BackLight.A / 255.0 * 4.0) : 0;
                            Utility.OsdOverlay.ShowBacklight(lvl);
                        }
                        catch
                        {
                        }
                        handled = true;
                        break;
                }
            }
            return IntPtr.Zero;
        }
    }
}
