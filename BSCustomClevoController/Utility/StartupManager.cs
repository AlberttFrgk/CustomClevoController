using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace BSCustomClevoController.Utility
{
    public static class StartupManager
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "CustomClevoController";
        private const string TaskName = "CustomClevoControllerStartup";

        public static string GetExecutablePath()
        {
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    string fileName = process.MainModule.FileName;
                    if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                    {
                        return Path.GetFullPath(fileName);
                    }
                }
            }
            catch
            {
            }

            try
            {
                string path = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }
            catch
            {
            }

            return AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + @"\BSCustomClevoController.exe";
        }

        public static bool IsRunAtStartupEnabled()
        {
            if (IsTaskScheduled())
            {
                return true;
            }

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue(AppName);
                        return val != null;
                    }
                }
            }
            catch
            {
            }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RunRegistryKey, false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue(AppName);
                        return val != null;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public static bool SetRunAtStartup(bool enable)
        {
            if (enable)
            {
                CreateStartupTask();

                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                    {
                        if (key != null)
                        {
                            string exePath = GetExecutablePath();
                            key.SetValue(AppName, "\"" + exePath + "\" /minimized");
                        }
                    }
                }
                catch
                {
                }
            }
            else
            {
                RemoveStartupTask();

                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                    {
                        if (key != null && key.GetValue(AppName) != null)
                        {
                            key.DeleteValue(AppName, false);
                        }
                    }
                }
                catch
                {
                }

                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RunRegistryKey, true))
                    {
                        if (key != null && key.GetValue(AppName) != null)
                        {
                            key.DeleteValue(AppName, false);
                        }
                    }
                }
                catch
                {
                }
            }

            return IsRunAtStartupEnabled() == enable;
        }

        public static void EnsureConfiguredCorrectly()
        {
            try
            {
                if (IsRunAtStartupEnabled())
                {
                    CreateStartupTask();
                }
            }
            catch
            {
            }
        }

        private static string BuildTaskXml(string exePath)
        {
            string exeDir = Path.GetDirectoryName(exePath) ?? string.Empty;
            return string.Format(
                "<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n" +
                "<Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">\r\n" +
                "  <RegistrationInfo>\r\n" +
                "    <Author>Custom Clevo Controller</Author>\r\n" +
                "    <Description>Starts Custom Clevo Controller with elevated privileges at user logon.</Description>\r\n" +
                "    <URI>\\{0}</URI>\r\n" +
                "  </RegistrationInfo>\r\n" +
                "  <Principals>\r\n" +
                "    <Principal id=\"Author\">\r\n" +
                "      <LogonType>InteractiveToken</LogonType>\r\n" +
                "      <RunLevel>HighestAvailable</RunLevel>\r\n" +
                "    </Principal>\r\n" +
                "  </Principals>\r\n" +
                "  <Settings>\r\n" +
                "    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>\r\n" +
                "    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>\r\n" +
                "    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>\r\n" +
                "    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>\r\n" +
                "    <Priority>4</Priority>\r\n" +
                "  </Settings>\r\n" +
                "  <Triggers>\r\n" +
                "    <LogonTrigger>\r\n" +
                "      <Enabled>true</Enabled>\r\n" +
                "    </LogonTrigger>\r\n" +
                "  </Triggers>\r\n" +
                "  <Actions Context=\"Author\">\r\n" +
                "    <Exec>\r\n" +
                "      <Command>{1}</Command>\r\n" +
                "      <Arguments>/minimized</Arguments>\r\n" +
                "      <WorkingDirectory>{2}</WorkingDirectory>\r\n" +
                "    </Exec>\r\n" +
                "  </Actions>\r\n" +
                "</Task>",
                TaskName,
                System.Security.SecurityElement.Escape(exePath),
                System.Security.SecurityElement.Escape(exeDir));
        }

        private static bool RegisterTaskViaCom(string xml)
        {
            try
            {
                Type serviceType = Type.GetTypeFromProgID("Schedule.Service");
                if (serviceType == null) return false;

                dynamic service = Activator.CreateInstance(serviceType);
                service.Connect();
                dynamic rootFolder = service.GetFolder(@"\");
                rootFolder.RegisterTask(TaskName, xml, 6, null, null, 3, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool RegisterTaskViaXmlCli(string xml)
        {
            string tempFile = null;
            try
            {
                tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xml");
                using (var fs = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(fs, System.Text.Encoding.Unicode))
                {
                    writer.Write(xml);
                }

                return ProcessRunner.Run(
                    ProcessRunner.ResolveSystemTool("schtasks.exe"),
                    string.Format("/create /f /tn \"{0}\" /xml \"{1}\"", TaskName, tempFile),
                    5000,
                    out _) == 0;
            }
            catch (Exception ex)
            {
                AppLogger.Log("StartupManager.RegisterTaskViaXmlCli", ex);
                return false;
            }
            finally
            {
                if (tempFile != null && File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch (Exception ex) { AppLogger.Log("StartupManager.DeleteTempXml", ex); }
                }
            }
        }

        private static bool RegisterTaskViaCli(string exePath)
        {
            try
            {
                string args = string.Format("/create /f /tn \"{0}\" /sc onlogon /rl highest /tr \"\\\"{1}\\\" /minimized\"", TaskName, exePath);
                return ProcessRunner.Run(ProcessRunner.ResolveSystemTool("schtasks.exe"), args, 5000, out _) == 0;
            }
            catch (Exception ex)
            {
                AppLogger.Log("StartupManager.RegisterTaskViaCli", ex);
                return false;
            }
        }

        private static bool CreateStartupTask()
        {
            string exePath = GetExecutablePath();
            string xml = BuildTaskXml(exePath);

            if (RegisterTaskViaCom(xml))
            {
                return true;
            }

            if (RegisterTaskViaXmlCli(xml))
            {
                return true;
            }

            return RegisterTaskViaCli(exePath);
        }

        private static bool RemoveStartupTask()
        {
            bool deleted = false;
            try
            {
                Type serviceType = Type.GetTypeFromProgID("Schedule.Service");
                if (serviceType != null)
                {
                    dynamic service = Activator.CreateInstance(serviceType);
                    service.Connect();
                    dynamic rootFolder = service.GetFolder(@"\");
                    rootFolder.DeleteTask(TaskName, 0);
                    deleted = true;
                }
            }
            catch
            {
            }

            try
            {
                if (ProcessRunner.Run(ProcessRunner.ResolveSystemTool("schtasks.exe"), string.Format("/delete /f /tn \"{0}\"", TaskName), 5000, out _) == 0)
                {
                    deleted = true;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("StartupManager.RemoveStartupTaskCli", ex);
            }

            return deleted;
        }

        private static bool IsTaskScheduled()
        {
            try
            {
                Type serviceType = Type.GetTypeFromProgID("Schedule.Service");
                if (serviceType != null)
                {
                    dynamic service = Activator.CreateInstance(serviceType);
                    service.Connect();
                    dynamic rootFolder = service.GetFolder(@"\");
                    dynamic task = rootFolder.GetTask(TaskName);
                    if (task != null && task.Enabled)
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            try
            {
                return ProcessRunner.Run(ProcessRunner.ResolveSystemTool("schtasks.exe"), string.Format("/query /tn \"{0}\"", TaskName), 5000, out _) == 0;
            }
            catch (Exception ex)
            {
                AppLogger.Log("StartupManager.IsTaskScheduledCli", ex);
                return false;
            }
        }
    }
}
