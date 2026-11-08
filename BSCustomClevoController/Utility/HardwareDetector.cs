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
            catch
            {
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
                    if (baseKey != null)
                    {
                        string[] subKeys = baseKey.GetSubKeyNames();

                        // 1. First priority: Discrete NVIDIA GPU
                        foreach (string sub in subKeys)
                        {
                            if (sub.StartsWith("0"))
                            {
                                using (RegistryKey subKey = baseKey.OpenSubKey(sub))
                                {
                                    if (subKey != null)
                                    {
                                        object descVal = subKey.GetValue("DriverDesc") ?? subKey.GetValue("ProviderName");
                                        if (descVal != null)
                                        {
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
                                        }
                                    }
                                }
                            }
                        }

                        // 2. Second priority: Discrete AMD Radeon GPU
                        foreach (string sub in subKeys)
                        {
                            if (sub.StartsWith("0"))
                            {
                                using (RegistryKey subKey = baseKey.OpenSubKey(sub))
                                {
                                    if (subKey != null)
                                    {
                                        object descVal = subKey.GetValue("DriverDesc") ?? subKey.GetValue("ProviderName");
                                        if (descVal != null)
                                        {
                                            string desc = descVal.ToString();
                                            if (desc.IndexOf("Radeon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                desc.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) >= 0)
                                            {
                                                gpuModel = desc;
                                                return GpuBrand.Amd;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // 3. Third priority: Intel Arc / Xe / Integrated
                        foreach (string sub in subKeys)
                        {
                            if (sub.StartsWith("0"))
                            {
                                using (RegistryKey subKey = baseKey.OpenSubKey(sub))
                                {
                                    if (subKey != null)
                                    {
                                        object descVal = subKey.GetValue("DriverDesc") ?? subKey.GetValue("ProviderName");
                                        if (descVal != null)
                                        {
                                            string desc = descVal.ToString();
                                            if (desc.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0)
                                            {
                                                gpuModel = desc;
                                                return GpuBrand.Intel;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return GpuBrand.Unknown;
        }
    }
}
