using System;
using Microsoft.Win32;

namespace BSCustomClevoController.Utility
{
    public enum CpuBrand
    {
        Intel,
        Amd,
        Unknown
    }

    public enum GpuBrand
    {
        Nvidia,
        Amd,
        Intel,
        Unknown
    }

    public static class HardwareDetector
    {
        public static CpuBrand DetectCpu(out string cpuModel)
        {
            cpuModel = "CPU";
            try
            {
                object val = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "");
                if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                {
                    cpuModel = val.ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("HardwareDetector.DetectCpu", ex);
            }

            if (cpuModel.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                cpuModel.IndexOf("Ryzen", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return CpuBrand.Amd;
            }
            if (cpuModel.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                cpuModel.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return CpuBrand.Intel;
            }

            return CpuBrand.Unknown;
        }

        public static GpuBrand DetectGpu(out string gpuModel)
        {
            gpuModel = "GPU";
            try
            {
                using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"))
                {
                    if (baseKey == null)
                        return GpuBrand.Unknown;

                    GpuBrand bestBrand = GpuBrand.Unknown;
                    string bestModel = null;

                    foreach (string sub in baseKey.GetSubKeyNames())
                    {
                        if (!sub.StartsWith("0"))
                            continue;

                        using (RegistryKey subKey = baseKey.OpenSubKey(sub))
                        {
                            object descVal = subKey?.GetValue("DriverDesc") ?? subKey?.GetValue("ProviderName");
                            if (descVal == null)
                                continue;

                            string desc = descVal.ToString();
                            if (desc.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                desc.IndexOf("GeForce", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                desc.IndexOf("RTX", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                desc.IndexOf("GTX", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                desc.IndexOf("Quadro", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                gpuModel = desc;
                                return GpuBrand.Nvidia;
                            }

                            if (bestBrand == GpuBrand.Unknown &&
                                (desc.IndexOf("Radeon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 desc.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                bestBrand = GpuBrand.Amd;
                                bestModel = desc;
                            }
                            else if (bestBrand == GpuBrand.Unknown &&
                                     desc.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                bestBrand = GpuBrand.Intel;
                                bestModel = desc;
                            }
                        }
                    }

                    if (bestBrand != GpuBrand.Unknown)
                    {
                        gpuModel = bestModel;
                        return bestBrand;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log("HardwareDetector.DetectGpu", ex);
            }

            return GpuBrand.Unknown;
        }
    }
}
