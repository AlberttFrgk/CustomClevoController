using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;

namespace BSCustomClevoController.Utility
{
    public static class AcpiDriverInstaller
    {
        private const string AcpiBridgeDevicePath = @"\\?\ACPI#CLV0001#1#{86994c74-ad43-4812-b7e7-0c420b5c5fd7}";
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID Luid;
            public uint Attributes;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLengthInBytes, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const uint EWX_REBOOT = 0x00000002;
        private const uint EWX_FORCE = 0x00000004;

        public static bool IsDriverWorking()
        {
            try
            {
                IntPtr hDevice = CreateFile(
                    AcpiBridgeDevicePath,
                    GENERIC_READ | GENERIC_WRITE,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    0,
                    IntPtr.Zero);

                if (hDevice != IntPtr.Zero && hDevice != new IntPtr(-1))
                {
                    CloseHandle(hDevice);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public static bool InstallDriver(out bool rebootRequired, bool force = false)
        {
            rebootRequired = false;
            if (!force && IsDriverWorking())
            {
                return true;
            }

            string driverDir = FindDriverDirectory();
            if (string.IsNullOrEmpty(driverDir))
            {
                return false;
            }

            string infPrimary = Path.Combine(driverDir, "AcpiBridge.inf");
            string infSecondary = Path.Combine(driverDir, "AcpiBridge1.Inf");

            if (!File.Exists(infPrimary))
            {
                return false;
            }

            int code1 = RunPnpUtil(string.Format("/add-driver \"{0}\" /install", infPrimary), out string out1);
            // 0 = Success, 3010 = Reboot required, 259 = ERROR_NO_MORE_ITEMS (package added, devices already current or waiting for restart)
            bool primaryOk = (code1 == 0 || code1 == 3010 || code1 == 259 ||
                              out1.IndexOf("successfully", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!primaryOk)
            {
                return false;
            }

            if (code1 == 3010)
            {
                rebootRequired = true;
            }

            if (File.Exists(infSecondary))
            {
                int code2 = RunPnpUtil(string.Format("/add-driver \"{0}\" /install", infSecondary), out _);
                if (code2 == 3010)
                {
                    rebootRequired = true;
                }
            }

            RunPnpUtil("/scan-devices");
            EnsureEventLogSource();

            Thread.Sleep(500);

            if (force)
            {
                StartupManager.SetRunAtStartup(true);
                rebootRequired = true;
                return true;
            }

            if (IsDriverWorking())
            {
                return true;
            }

            rebootRequired = true;
            return true;
        }

        public static bool InstallDriver(out bool rebootRequired)
        {
            return InstallDriver(out rebootRequired, force: false);
        }

        public static bool EnsureDriverInstalled()
        {
            return InstallDriver(out _, force: false);
        }

        private static void EnableShutdownPrivilege()
        {
            try
            {
                IntPtr hToken;
                if (OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken))
                {
                    TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
                    {
                        PrivilegeCount = 1,
                        Attributes = SE_PRIVILEGE_ENABLED
                    };
                    if (LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, out tp.Luid))
                    {
                        AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                    }
                    CloseHandle(hToken);
                }
            }
            catch
            {
            }
        }

        public static void RestartComputer()
        {
            try
            {
                EnableShutdownPrivilege();

                string shutdownPath = GetSystemToolPath("shutdown.exe");
                var psi = new ProcessStartInfo
                {
                    FileName = shutdownPath,
                    Arguments = "/r /t 0 /f",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit(3000);
                }

                ExitWindowsEx(EWX_REBOOT | EWX_FORCE, 0x00020000 | 0x00000004 | 0x40000000);
            }
            catch (Exception ex)
            {
                App.LogErrorToFile("RestartComputer", ex);
            }

            try
            {
                var psiPs = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command Restart-Computer -Force",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psiPs))
                {
                    proc?.WaitForExit(3000);
                }
            }
            catch
            {
            }
        }

        public static string FindDriverDirectory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] searchPaths = new string[]
            {
                Path.Combine(baseDir, "AcpiBridge"),
                Path.Combine(baseDir, "..", "AcpiBridge"),
                Path.Combine(baseDir, "..", "..", "AcpiBridge"),
                Path.Combine(baseDir, "..", "..", "Drivers", "AcpiBridge")
            };

            foreach (string searchPath in searchPaths)
            {
                try
                {
                    string infPath = Path.Combine(searchPath, "AcpiBridge.inf");
                    if (File.Exists(infPath))
                    {
                        return Path.GetFullPath(searchPath);
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        public static bool UninstallDriver()
        {
            try
            {
                StartupManager.SetRunAtStartup(false);

                string scPath = GetSystemToolPath("sc.exe");
                RunProcess(scPath, "stop AcpiBridge");
                RunProcess(scPath, "delete AcpiBridge");
                RunProcess(scPath, "stop AcpiBridge1");
                RunProcess(scPath, "delete AcpiBridge1");

                RunPnpUtil("/remove-device /deviceid \"ACPI\\CLV0001\"");
                RunPnpUtil("/remove-device /deviceid \"ACPI\\CLV0002\"");
                RunPnpUtil("/remove-device \"ACPI\\CLV0001\\1\"");

                DeletePublishedDriversMatching("acpibridge");

                RunPnpUtil("/scan-devices");
                StartupManager.SetRunAtStartup(false);
                return true;
            }
            catch (Exception ex)
            {
                App.LogErrorToFile("UninstallDriver", ex);
                return false;
            }
        }

        private static void DeletePublishedDriversMatching(string pattern)
        {
            try
            {
                string pnpPath = GetSystemToolPath("pnputil.exe");
                var psi = new ProcessStartInfo
                {
                    FileName = pnpPath,
                    Arguments = "/enum-drivers",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        string output = proc.StandardOutput.ReadToEnd();
                        proc.StandardError.ReadToEnd();
                        proc.WaitForExit(10000);

                        var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                        string currentPublishedName = null;

                        foreach (string line in lines)
                        {
                            string trimmed = line.Trim();
                            if (trimmed.StartsWith("Published Name:", StringComparison.OrdinalIgnoreCase))
                            {
                                currentPublishedName = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
                            }
                            else if (trimmed.StartsWith("Original Name:", StringComparison.OrdinalIgnoreCase))
                            {
                                string origName = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
                                if (origName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0 && !string.IsNullOrEmpty(currentPublishedName))
                                {
                                    RunPnpUtil(string.Format("/delete-driver {0} /uninstall /force", currentPublishedName));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.LogErrorToFile("DeletePublishedDriversMatching", ex);
            }
        }

        private static int RunProcess(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        proc.WaitForExit(10000);
                        return proc.ExitCode;
                    }
                }
            }
            catch
            {
            }
            return -1;
        }

        private static string GetSystemToolPath(string toolName)
        {
            try
            {
                string systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string fullPath = Path.Combine(systemDir, toolName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            catch
            {
            }
            return toolName;
        }

        private static int RunPnpUtil(string arguments, out string output)
        {
            output = string.Empty;
            try
            {
                string pnpPath = GetSystemToolPath("pnputil.exe");
                var psi = new ProcessStartInfo
                {
                    FileName = pnpPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        string stdout = proc.StandardOutput.ReadToEnd();
                        string stderr = proc.StandardError.ReadToEnd();
                        proc.WaitForExit(15000);
                        output = (stdout + "\n" + stderr).Trim();
                        return proc.ExitCode;
                    }
                }
            }
            catch (Exception ex)
            {
                App.LogErrorToFile("RunPnpUtil", ex);
            }

            return -1;
        }

        private static int RunPnpUtil(string arguments)
        {
            return RunPnpUtil(arguments, out _);
        }

        private static void EnsureEventLogSource()
        {
            try
            {
                if (!EventLog.SourceExists("PowerBIOSServer_Out"))
                {
                    EventLog.CreateEventSource("PowerBIOSServer_Out", "OutLog");
                }
            }
            catch
            {
            }
        }
    }
}
