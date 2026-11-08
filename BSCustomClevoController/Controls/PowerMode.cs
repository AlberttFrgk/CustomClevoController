using System.Runtime.InteropServices;
using BSCustomClevoController.Utility;

namespace BSCustomClevoController.Controls
{
    public class PowerMode
    {
        private static int SetDCHU_Data(int command, byte[] buffer, int length)
        {
            return InsydeDriver.SetDCHU_Data(command, buffer, length);
        }

        private static int WriteAppSettings(int page, int offset, int length, ref byte buffer)
        {
            return InsydeDriver.WriteAppSettings(page, offset, length, ref buffer);
        }

        public static int ReadAppSettings(int page, int offset, int length, ref byte buffer)
        {
            return InsydeDriver.ReadAppSettings(page, offset, length, ref buffer);
        }

        public static Mode GetStatus()
        {
            try
            {
                object regVal = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "PowerMode", null);
                if (regVal is int intMode)
                {
                    return LookupMode((byte)intMode);
                }

                // Default to Entertainment mode for new installs
                SetPowerMode(Mode.Entertainment);
                return Mode.Entertainment;
            }
            catch
            {
                return Mode.Entertainment;
            }
        }

        public enum Mode
        {
            Quiet = 0,
            Powersaving = 1,
            Performance = 2,
            Entertainment = 3,
            Unknown = 4,
        }

        static Mode LookupMode(byte mode)
        {
            switch (mode)
            {
                case 0: return Mode.Quiet;
                case 1: return Mode.Powersaving;
                case 2: return Mode.Performance;
                case 3: return Mode.Entertainment;
                default: return Mode.Entertainment;
            }
        }

        public static void SetPowerMode(Mode mode)
        {
            try
            {
                byte value = (byte)mode;
                SetDCHU_Data(121, new byte[4] { value, 0, 0, 25 }, 4);
                WriteAppSettings(1, 1, 1, ref value);

                try
                {
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\BSCustomClevoController", "PowerMode", (int)mode);
                }
                catch
                {
                }

                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        SystemPreferences.SetDiscreteGpuState(mode != Mode.Powersaving);
                    }
                    catch
                    {
                    }
                });
            }
            catch
            {
            }
        }

        private static System.DateTime _lastPowerCycleTime = System.DateTime.MinValue;
        private static readonly object _powerCycleLock = new object();

        public static Mode CyclePowerMode()
        {
            lock (_powerCycleLock)
            {
                if (System.DateTime.UtcNow < _lastPowerCycleTime.AddMilliseconds(400))
                {
                    return GetStatus();
                }
                _lastPowerCycleTime = System.DateTime.UtcNow;

                try
                {
                    var current = GetStatus();
                    Mode next;
                    switch (current)
                    {
                        case Mode.Quiet:
                            next = Mode.Powersaving;
                            break;
                        case Mode.Powersaving:
                            next = Mode.Entertainment;
                            break;
                        case Mode.Entertainment:
                            next = Mode.Performance;
                            break;
                        case Mode.Performance:
                        default:
                            next = Mode.Quiet;
                            break;
                    }
                    SetPowerMode(next);
                    OsdOverlay.ShowPowerMode(next);

                    System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new System.Action(() =>
                    {
                        var mainWin = System.Windows.Application.Current.MainWindow as MainWindow;
                        mainWin?.SyncPowerProfileUI(next);
                    }));

                    return next;
                }
                catch
                {
                    return Mode.Entertainment;
                }
            }
        }
    }
}
