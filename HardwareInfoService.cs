using System;
using System.Management;

namespace EasyTune
{
    public static class HardwareInfoService
    {
        public static string GetSummary()
        {
            string cpu = GetWMI("Win32_Processor", "Name");
            string mem = GetMemoryInfo();        // 修正：调用正确的方法名
            string mb = GetWMI("Win32_BaseBoard", "Product");
            string gpu = GetWMI("Win32_VideoController", "Name");
            string disk = GetDisk();
            return $"CPU: {cpu}\r\n内存: {mem}\r\n主板: {mb}\r\n显卡: {gpu}\r\n硬盘: {disk}";
        }

        private static string GetWMI(string cls, string prop)
        {
            try
            {
                using (var s = new ManagementObjectSearcher($"SELECT {prop} FROM {cls}"))
                    foreach (ManagementObject o in s.Get())
                        return o[prop]?.ToString().Trim() ?? "未知";
            }
            catch { return "读取失败"; }
            return "未知";
        }

        private static string GetMemoryInfo()
        {
            double totalGB = 0;
            string speedInfo = "";
            string ddrType = "";

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Capacity, Speed, SMBIOSMemoryType FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (obj["Capacity"] != null)
                            totalGB += Convert.ToDouble(obj["Capacity"]) / (1024 * 1024 * 1024);

                        if (string.IsNullOrEmpty(speedInfo) && obj["Speed"] != null)
                            speedInfo = obj["Speed"].ToString() + " MHz";

                        if (string.IsNullOrEmpty(ddrType) && obj["SMBIOSMemoryType"] != null)
                        {
                            uint type = Convert.ToUInt32(obj["SMBIOSMemoryType"]);
                            ddrType = MapMemoryTypeToDDR(type);
                        }
                    }
                }

                // 如果 SMBIOSMemoryType 未返回有效值，尝试使用 MemoryType
                if (string.IsNullOrEmpty(ddrType))
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT MemoryType FROM Win32_PhysicalMemory"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            if (obj["MemoryType"] != null)
                            {
                                uint type = Convert.ToUInt32(obj["MemoryType"]);
                                ddrType = MapMemoryTypeToDDR(type);
                                break;
                            }
                        }
                    }
                }
            }
            catch { }

            string result = $"{totalGB:F1} GB";
            if (!string.IsNullOrEmpty(ddrType))
                result += $" {ddrType}";
            if (!string.IsNullOrEmpty(speedInfo))
                result += $" ({speedInfo})";

            return result;
        }

        private static string MapMemoryTypeToDDR(uint type)
        {
            // 合并两种来源的映射表，去除重复值
            switch (type)
            {
                // SMBIOSMemoryType 常见值
                case 18: return "DDR";
                case 19: return "DDR2";
                case 20: return "DDR2";     // 某些来源中 20 是 DDR2
                case 21: return "DDR3";
                case 22: return "DDR4";
                case 23: return "DDR5";
                case 24: return "DDR3";     // 另一种 DDR3 代码
                case 26: return "DDR4";     // 另一种 DDR4 代码
                case 34: return "DDR5";     // 另一种 DDR5 代码
                default: return "";
            }
        }

        private static string GetDisk()
        {
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Model, Size FROM Win32_DiskDrive WHERE Index=0"))
                    foreach (ManagementObject o in s.Get())
                    {
                        string m = o["Model"]?.ToString().Trim() ?? "未知";
                        double sz = 0;
                        if (o["Size"] != null) sz = Convert.ToDouble(o["Size"]) / (1024 * 1024 * 1024);
                        return $"{m} ({sz:F0} GB)";
                    }
            }
            catch { }
            return "未知";
        }
    }
}