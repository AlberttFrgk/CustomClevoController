using System;
using System.Runtime.InteropServices;
using BSCustomClevoController.Utility;

namespace BSCustomClevoController.Controls
{
    public static class CpuPowerControl
    {
        private static int ReadAppSettings(int page, int offset, int length, ref byte buffer)
        {
            return InsydeDriver.ReadAppSettings(page, offset, length, ref buffer);
        }

        private static int WriteAppSettings(int page, int offset, int length, ref byte buffer)
        {
            return InsydeDriver.WriteAppSettings(page, offset, length, ref buffer);
        }

        private static string _cpuModel;
        private static int _cachedCpuPowerOffset = 0;
        private static bool _powerLoaded = false;

        public static bool IsSupported
        {
            get
            {
                return Capabilities.IsSupported("SupportCPUOC");
            }
        }

        public static string CpuModel
        {
            get
            {
                if (string.IsNullOrEmpty(_cpuModel))
                {
                    HardwareDetector.DetectCpu(out _cpuModel);
                }
                return string.IsNullOrWhiteSpace(_cpuModel) ? "Processor" : _cpuModel;
            }
        }

        public static void ResetDetection()
        {
            _cpuModel = null;
            _powerLoaded = false;
        }

        public static int GetPowerOffset()
        {
            if (_powerLoaded) return _cachedCpuPowerOffset;
            if (!IsSupported)
            {
                _cachedCpuPowerOffset = 0;
                _powerLoaded = true;
                return 0;
            }

            try
            {
                byte[] array = new byte[4];
                ReadAppSettings(6, 33, 4, ref array[0]);
                int val = BitConverter.ToInt32(array, 0);
                if (val >= 0 && val <= 25)
                {
                    _cachedCpuPowerOffset = val;
                }
                else
                {
                    _cachedCpuPowerOffset = 0;
                }
            }
            catch
            {
                _cachedCpuPowerOffset = 0;
            }

            _powerLoaded = true;
            return _cachedCpuPowerOffset;
        }

        public static void SetPowerOffset(int offsetWatts)
        {
            if (!IsSupported) return;

            if (offsetWatts < 0) offsetWatts = 0;
            if (offsetWatts > 25) offsetWatts = 25;

            _cachedCpuPowerOffset = offsetWatts;
            _powerLoaded = true;

            try
            {
                // DCHU AppSettings Page 6:
                // Offset 32: IsSaved flag (1)
                // Offset 33: PL1 (4 bytes)
                // Offset 37: PL2 (4 bytes)
                byte saveFlag = 1;
                WriteAppSettings(6, 32, 1, ref saveFlag);

                byte[] pl1Bytes = BitConverter.GetBytes(offsetWatts);
                WriteAppSettings(6, 33, 4, ref pl1Bytes[0]);

                byte[] pl2Bytes = BitConverter.GetBytes(offsetWatts);
                WriteAppSettings(6, 37, 4, ref pl2Bytes[0]);
            }
            catch
            {
            }
        }
    }
}
