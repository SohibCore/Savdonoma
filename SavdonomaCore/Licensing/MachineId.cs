using Microsoft.Win32;
using System.Management;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace Savdonoma.Core.Licensing
{
    public static class MachineId
    {
        public static string Get()
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Machine ID faqat Windows tizimida olinadi.");

            var guid = GetMachineGuid();
            var board = GetWmi("Win32_BaseBoard", "SerialNumber");

            if (string.IsNullOrWhiteSpace(guid) && string.IsNullOrWhiteSpace(board))
                throw new InvalidOperationException("Kompyuter identifikatorini olishning imkoni bo'lmadi.");

            var raw = $"{guid}|{board}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            var hex = Convert.ToHexString(hash)[..16];
            return string.Join("-", Enumerable.Range(0, 4).Select(i => hex.Substring(i * 4, 4)));
        }

        [SupportedOSPlatform("windows")]
        private static string GetMachineGuid()
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            return key?.GetValue("MachineGuid")?.ToString() ?? "";
        }

        [SupportedOSPlatform("windows")]
        private static string GetWmi(string wmiClass, string property)
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");

            foreach (var obj in searcher.Get())
            {
                var value = obj[property]?.ToString()?.Trim() ?? "";
                if (value.Length > 3 &&
                    !value.Contains("O.E.M", StringComparison.OrdinalIgnoreCase) &&
                    !value.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                    !value.Equals("Default string", StringComparison.OrdinalIgnoreCase))
                    return value;
            }

            return "";
        }
    }
}
