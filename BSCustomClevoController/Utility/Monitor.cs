using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace BSCustomClevoController.Utility
{
    class MonitorControl
    {
        private const uint WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private const uint MOUSEEVENTF_MOVE = 0x0001;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        private static IntPtr GetSafeWindowHandle()
        {
            try
            {
                if (Application.Current != null && Application.Current.MainWindow != null)
                {
                    IntPtr handle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                    if (handle != IntPtr.Zero)
                    {
                        return handle;
                    }
                }
            }
            catch
            {
            }
            return GetForegroundWindow();
        }

        public static void ChangeMonitorState(MonitorMode mode)
        {
            IntPtr hwnd = GetSafeWindowHandle();
            if (hwnd != IntPtr.Zero)
            {
                SendMessage(hwnd, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)mode);
            }
            else
            {
                DefWindowProc(new IntPtr(0xFFFF), WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)mode);
            }
        }

        public static void MonitorOff()
        {
            ChangeMonitorState(MonitorMode.MONITOR_OFF);
        }

        public static void MonitorOn()
        {
            Task.Run(() =>
            {
                ChangeMonitorState(MonitorMode.MONITOR_ON);
                mouse_event(MOUSEEVENTF_MOVE, 0, 1, 0, UIntPtr.Zero);
                mouse_event(MOUSEEVENTF_MOVE, 0, -1, 0, UIntPtr.Zero);
            });
        }

        public static void MonitorStandBy()
        {
            ChangeMonitorState(MonitorMode.MONITOR_STANBY);
        }

        public enum MonitorMode
        {
            MONITOR_ON = -1,
            MONITOR_STANBY = 1,
            MONITOR_OFF = 2,
        }
    }
}