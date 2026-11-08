using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using ControlzEx.Theming;
using MahApps.Metro.Theming;

namespace BSCustomClevoController
{
    public partial class App : Application
    {
        private const string AppMutexName = "BSCustomClevoController_SingleInstance_Mutex";
        private const string WakeEventName = "BSCustomClevoController_WakeEvent";

        private static Mutex _appMutex;
        private static EventWaitHandle _wakeEvent;
        private static RegisteredWaitHandle _waitRegistration;

        public App()
        {
            DispatcherUnhandledException += (s, args) =>
            {

                string logPath = LogErrorToFile("DispatcherUnhandledException", args.Exception);
                MessageBox.Show(

                    "An unexpected error occurred (saved to " + logPath + "):\n\n" + args.Exception.Message + "\n\nStack Trace:\n" + args.Exception.StackTrace,
                    "Application Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
    
                    string logPath = LogErrorToFile("UnhandledException", ex);
                    MessageBox.Show(
                        "A critical error occurred (saved to " + logPath + "):\n\n" + ex.Message + "\n\nStack Trace:\n" + ex.StackTrace,
                        "Critical Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            try
            {
                var mutexSecurity = new MutexSecurity();
                mutexSecurity.AddAccessRule(new MutexAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    MutexRights.FullControl,
                    AccessControlType.Allow));
                _appMutex = new Mutex(true, AppMutexName, out createdNew, mutexSecurity);
            }
            catch
            {
                _appMutex = new Mutex(true, AppMutexName, out createdNew);
            }

            if (!createdNew)
            {
                // Signal running instance to bring window to foreground
                EventWaitHandle wakeHandle;
                if (EventWaitHandle.TryOpenExisting(WakeEventName, out wakeHandle))
                {
                    using (wakeHandle)
                    {
                        wakeHandle.Set();
                    }
                }

                // Exit quietly without warning or notification
                Shutdown();
                return;
            }

            try
            {
                var eventSecurity = new EventWaitHandleSecurity();
                eventSecurity.AddAccessRule(new EventWaitHandleAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    EventWaitHandleRights.FullControl,
                    AccessControlType.Allow));
                bool eventCreated;
                _wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, WakeEventName, out eventCreated, eventSecurity);
            }
            catch
            {
                _wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, WakeEventName);
            }

            _waitRegistration = ThreadPool.RegisterWaitForSingleObject(_wakeEvent, OnWakeRequested, null, -1, false);

            var theme = ThemeManager.Current.AddLibraryTheme(new LibraryTheme(
                new Uri("pack://application:,,,/BSCustomClevoController;component/Dark.BlueBlack.xaml"),
                MahAppsLibraryThemeProvider.DefaultInstance));

            if (!Utility.AcpiDriverInstaller.IsDriverWorking())
            {
                var confirmResult = MessageBox.Show(
                    "Custom Clevo Controller requires the AcpiBridge driver to control fans, power modes, and keyboard lighting.\n\nInstalling this driver requires restarting your PC.\n\nWould you like to install the driver and restart your PC?",
                    "AcpiBridge Driver Setup",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes)
                {
                    Shutdown();
                    return;
                }

                bool rebootRequired;
                bool installed = Utility.AcpiDriverInstaller.InstallDriver(out rebootRequired);

                if (installed)
                {
                    Utility.StartupManager.SetRunAtStartup(true);
                    var restartChoice = MessageBox.Show(
                        "AcpiBridge driver installed successfully.\n\nWould you like to restart your PC now to finish setup?",
                        "Restart PC",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (restartChoice == MessageBoxResult.Yes)
                    {
                        Utility.AcpiDriverInstaller.RestartComputer();
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Failed to install the AcpiBridge driver. Please ensure the driver files exist in the AcpiBridge folder and try again.",
                        "Driver Installation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                Shutdown();
                return;
            }

            base.OnStartup(e);

            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.DoNotSync;
            ThemeManager.Current.ChangeTheme(this, theme);
        }

        private void OnWakeRequested(object state, bool timedOut)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (MainWindow is MainWindow mainWindow)
                {
                    mainWindow.BringToForeground();
                }
            }));
        }

        private static bool _hasCleanedUp = false;
        private static readonly object _cleanupLock = new object();

        public static void CleanupOnExit()
        {
            lock (_cleanupLock)
            {
                if (_hasCleanedUp) return;
                _hasCleanedUp = true;

                try
                {
                    if (Controls.KeyboardEffects.IsAnySoftwareEffectActive())
                    {
                        Controls.KeyboardEffects.DisableSoftwareAnimationForExit();
                    }
                }
                catch
                {
                }

                try
                {
                    Utility.SystemPreferences.Shutdown();
                }
                catch
                {
                }
            }
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            CleanupOnExit();
            base.OnSessionEnding(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            CleanupOnExit();

            if (_waitRegistration != null)
            {
                _waitRegistration.Unregister(null);
                _waitRegistration = null;
            }

            if (_wakeEvent != null)
            {
                _wakeEvent.Dispose();
                _wakeEvent = null;
            }

            if (_appMutex != null)
            {
                try { _appMutex.ReleaseMutex(); } catch { }
                _appMutex.Dispose();
                _appMutex = null;
            }

            base.OnExit(e);
        }

        public static string LogErrorToFile(string context, Exception ex)
        {
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
            try
            {
                string entry = string.Format(
                    "[{1:yyyy-MM-dd HH:mm:ss}] {2}{0}{3}{0}{0}",
                    Environment.NewLine,
                    DateTime.Now,
                    context,
                    ex);
                System.IO.File.AppendAllText(logPath, entry);
            }
            catch
            {
            }
            return logPath;
        }
    }
}
