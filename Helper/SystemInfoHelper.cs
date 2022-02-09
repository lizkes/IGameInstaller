using System;

using IGameInstaller.Model;

namespace IGameInstaller.Helper
{
    public class SystemInfoHelper
    {
        public static SystemVersion StringToSystemVersion(string svString)
        {
            var ok = Enum.TryParse(svString, out SystemVersion systemVersion);
            if (ok)
            {
                return systemVersion;
            }
            else
            {
                return SystemVersion.Unknown;
            }
        }
        public static string SystemVersionToReadable(SystemVersion systemVersion)
        {
            return systemVersion switch
            {
                SystemVersion.Win95x32 => "Windows95 32位",
                SystemVersion.Win95x64 => "Windows95 64位",
                SystemVersion.Win98x32 => "Windows98 32位",
                SystemVersion.Win98x64 => "Windows98 64位",
                SystemVersion.WinMex32 => "WindowsMe 32位",
                SystemVersion.WinMex64 => "WindowsMe 64位",
                SystemVersion.WinNTx32 => "WindowsNT 32位",
                SystemVersion.WinNTx64 => "WindowsNT 64位",
                SystemVersion.Win2000x32 => "Windows2000 32位",
                SystemVersion.Win2000x64 => "Windows2000 64位",
                SystemVersion.WinXPx32 => "WindowsXP 32位",
                SystemVersion.WinXPx64 => "WindowsXP 64位",
                SystemVersion.Win2003x32 => "Windows2003 32位",
                SystemVersion.Win2003x64 => "Windows2003 64位",
                SystemVersion.WinVistax32 => "WindowsVista 32位",
                SystemVersion.WinVistax64 => "WindowsVista 64位",
                SystemVersion.Win7x32 => "Windows7 32位",
                SystemVersion.Win7x64 => "Windows7 64位",
                SystemVersion.Win8x32 => "Windows8 32位",
                SystemVersion.Win8x64 => "Windows8 64位",
                SystemVersion.Win8_1x32 => "Windows8.1 32位",
                SystemVersion.Win8_1x64 => "Windows8.1 64位",
                SystemVersion.Win10x32 => "Windows10 32位",
                SystemVersion.Win10x64 => "Windows10 64位",
                SystemVersion.Win11x32 => "Windows11 32位",
                SystemVersion.Win11x64 => "Windows11 64位",
                _ => "未知",
            };
        }

        public static SystemVersion GetCurrentSystemVersion()
        {
            var platform = Environment.OSVersion.Platform;
            var version = Environment.OSVersion.Version;

            if (platform == PlatformID.Win32NT && version.Major == 10 && version.Minor == 0)
            {
                if (Environment.Is64BitOperatingSystem) return SystemVersion.Win10x64;
                return SystemVersion.Win10x32;
            }
            else if (platform == PlatformID.Win32NT && version.Major == 6 && version.Minor == 3)
            {
                if (Environment.Is64BitOperatingSystem) return SystemVersion.Win8_1x64;
                return SystemVersion.Win8_1x32;
            }
            else if (platform == PlatformID.Win32NT && version.Major == 6 && version.Minor == 2)
            {
                if (Environment.Is64BitOperatingSystem) return SystemVersion.Win8x64;
                return SystemVersion.Win8x32;
            }
            else if (platform == PlatformID.Win32NT && version.Major == 6 && version.Minor == 1)
            {
                if (Environment.Is64BitOperatingSystem) return SystemVersion.Win7x64;
                return SystemVersion.Win7x32;
            }
            else if (platform == PlatformID.Win32NT && version.Major == 6 && version.Minor == 0)
            {
                if (Environment.Is64BitOperatingSystem) return SystemVersion.WinVistax64;
                return SystemVersion.WinVistax32;
            }
            else if (platform == PlatformID.Win32NT && version.Major == 5 && version.Minor == 2)
            {
                if (Environment.Is64BitOperatingSystem) return SystemVersion.Win2003x64;
                return SystemVersion.Win2003x32;
            }
            else if (platform == PlatformID.Win32NT && version.Major == 5 && version.Minor == 1)
            {
                if (Environment.Is64BitOperatingSystem) return SystemVersion.WinXPx64;
                return SystemVersion.WinXPx32;
            }
            else if (platform == PlatformID.Win32NT && version.Major == 5 && version.Minor == 0)
            {
                if (Environment.Is64BitOperatingSystem) return SystemVersion.Win2000x64;
                return SystemVersion.Win2000x32;
            }
            else if (platform == PlatformID.Win32NT && version.Major == 4 && version.Minor == 0)
            {
                if (Environment.Is64BitOperatingSystem) return SystemVersion.WinNTx64;
                return SystemVersion.WinNTx32;
            }
            else if (platform == PlatformID.Win32Windows && version.Major == 4 && version.Minor == 90)
            {
                if (Environment.Is64BitOperatingSystem) return SystemVersion.WinMex64;
                return SystemVersion.WinMex32;
            }
            else if (platform == PlatformID.Win32Windows && version.Major == 4 && version.Minor == 10)
            {
                if (Environment.Is64BitOperatingSystem) return SystemVersion.Win98x64;
                return SystemVersion.Win98x32;
            }
            else if (platform == PlatformID.Win32Windows && version.Major == 4 && version.Minor == 0)
            {
                if (Environment.Is64BitOperatingSystem) return SystemVersion.Win95x64;
                return SystemVersion.Win95x32;
            }
            else
            {
                if (version.Build >= 22000)
                {
                    if (Environment.Is64BitOperatingSystem) return SystemVersion.Win11x64;
                    return SystemVersion.Win11x32;
                }
                else
                {
                    return SystemVersion.Unknown;
                }
            }
        }
    }
}
