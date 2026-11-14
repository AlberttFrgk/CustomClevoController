using System;
using System.IO;

namespace BSCustomClevoController.Utility
{
    public static class AppLogger
    {
        private const long MaxLogBytes = 2 * 1024 * 1024;
        private static readonly object LogLock = new object();

        public static string Log(string context, Exception ex)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string logPath = Path.Combine(baseDir, "error_log.txt");
            try
            {
                string entry = string.Format(
                    "[{1:yyyy-MM-dd HH:mm:ss}] {2}{0}{3}{0}{0}",
                    Environment.NewLine,
                    DateTime.Now,
                    context,
                    ex);
                lock (LogLock)
                {
                    var info = new FileInfo(logPath);
                    if (info.Exists && info.Length > MaxLogBytes)
                    {
                        string backupPath = Path.Combine(baseDir, "error_log.old.txt");
                        if (File.Exists(backupPath))
                        {
                            File.Delete(backupPath);
                        }
                        File.Move(logPath, backupPath);
                    }
                    File.AppendAllText(logPath, entry);
                }
            }
            catch
            {
                // Cannot recover if log file itself is unwritable
            }
            return logPath;
        }
    }
}

