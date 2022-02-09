using System;
using System.IO;

using IGameInstaller.Model;

namespace IGameInstaller.Helper
{
    public class DependHelper
    {
        public static Depend StringToDepend(string dependString)
        {
            var ok = Enum.TryParse(dependString, out Depend depend);
            if (ok)
            {
                return depend;
            }
            else
            {
                return Depend.Unknown;
            }
        }

        public static string DependToReadable(Depend depend)
        {
            return depend switch
            {
                Depend.NetFramwork4_8 => ".NET框架 4.8",
                Depend.VC2005 => "Visual C++ 2005",
                Depend.VC2008 => "Visual C++ 2008",
                Depend.VC2010 => "Visual C++ 2010",
                Depend.VC2012 => "Visual C++ 2012",
                Depend.VC2013 => "Visual C++ 2013",
                Depend.VC2015 => "Visual C++ 2015",
                Depend.VC2017 => "Visual C++ 2017",
                Depend.VC2015to2022 => "Visual C++ 2015-2022",
                Depend.DirectX9 => "DirectX 9",
                Depend.ParadoxLauncher => "Paradox启动器",
                Depend.ParadoxLauncherCrack => "Paradox启动器破解补丁",
                _ => "未知",
            };
        }
        
        public static int DependToResourcId(Depend depend)
        {
            return depend switch
            {
                Depend.NetFramwork4_8 => 9,
                Depend.VC2005 => 14,
                Depend.VC2008 => 15,
                Depend.VC2010 => 16,
                Depend.VC2012 => 17,
                Depend.VC2013 => 18,
                Depend.VC2015 => 19,
                Depend.VC2017 => 20,
                Depend.VC2015to2022 => 21,
                Depend.DirectX9 => 22,
                Depend.ParadoxLauncher => 23,
                Depend.ParadoxLauncherCrack => 24,
                _ => -1,
            };
        }

        public static (bool, bool) NeedInstall(Depend depend)
        {
            var is64BitSystem = Environment.Is64BitOperatingSystem;
            switch (depend)
            {
                case Depend.NetFramwork4_8: return (true, true);
                case Depend.VC2005:
                    {
                        var exist32bit = RegistryHelper.GetRegistry(@"SOFTWARE\Classes\Installer\Products\c1c4f01781cc94c4c8fb1542c0981a2a", "Version") != null;
                        var exist64bit = RegistryHelper.GetRegistry(@"SOFTWARE\Classes\Installer\Products\1af2a8da7e60d0b429d7e6453b3d0182", "Version") != null;
                        return (!exist32bit, is64BitSystem && !exist64bit);
                    }
                case Depend.VC2008:
                    {
                        var exist32bit = RegistryHelper.GetRegistry(@"SOFTWARE\Classes\Installer\Products\6E815EB96CCE9A53884E7857C57002F0", "Version") != null;
                        var exist64bit = RegistryHelper.GetRegistry(@"SOFTWARE\Classes\Installer\Products\67D6ECF5CD5FBA732B8B22BAC8DE1B4D", "Version") != null;
                        return (!exist32bit, is64BitSystem && !exist64bit);
                    }
                case Depend.VC2010:
                    {
                        var exist32bit = RegistryHelper.GetRegistry(@"SOFTWARE\Classes\Installer\Products\1D5E3C0FEDA1E123187686FED06E995A", "Version") != null;
                        var exist64bit = RegistryHelper.GetRegistry(@"SOFTWARE\Classes\Installer\Products\1926E8D15D0BCE53481466615F760A7F", "Version") != null;
                        return (!exist32bit, is64BitSystem && !exist64bit);
                    }
                case Depend.VC2012:
                    {
                        var exist32bit = RegistryHelper.GetRegistry(@"SOFTWARE\Classes\Installer\Dependencies\{33d1fd90-4274-48a1-9bc1-97e33d9c2d6f}", "Version") != null;
                        var exist64bit = RegistryHelper.GetRegistry(@"SOFTWARE\Classes\Installer\Dependencies\{ca67548a-5ebe-413a-b50c-4b9ceb6d66c6}", "Version") != null;
                        return (!exist32bit, is64BitSystem && !exist64bit);
                    }
                case Depend.VC2013:
                    {
                        var exist32bit = RegistryHelper.GetRegistry(@"SOFTWARE\Classes\Installer\Dependencies\{f65db027-aff3-4070-886a-0d87064aabb1}", "Version") != null;
                        var exist64bit = RegistryHelper.GetRegistry(@"SOFTWARE\Classes\Installer\Dependencies\{050d4fc8-5d48-4b8f-8972-47c82c46020f}", "Version") != null;
                        return (!exist32bit, is64BitSystem && !exist64bit);
                    }
                case Depend.VC2015:
                    {
                        var (needInstall32bit, needInstall64bit) = NeedInstall(Depend.VC2015to2022);
                        var exist32bit = RegistryHelper.GetRegistry(@"SOFTWARE\Classes\Installer\Dependencies\{e2803110-78b3-4664-a479-3611a381656a}", "Version") != null;
                        var exist64bit = RegistryHelper.GetRegistry(@"SOFTWARE\Classes\Installer\Dependencies\{d992c12e-cab2-426f-bde3-fb8c53950b0d}", "Version") != null;
                        return (!exist32bit && needInstall32bit, is64BitSystem && !exist64bit && needInstall64bit);
                    }
                case Depend.VC2017:
                    {
                        var (needInstall32bit, needInstall64bit) = NeedInstall(Depend.VC2015to2022);
                        var exist32bit = RegistryHelper.GetRegistry(@"Installer\Dependencies\VC,redist.x86,x86,14.16,bundle", "Version", RegistryHelper.RegisterType.classesRoot) != null;
                        var exist64bit = RegistryHelper.GetRegistry(@"Installer\Dependencies\VC,redist.x64,amd64,14.16,bundle", "Version", RegistryHelper.RegisterType.classesRoot) != null;
                        return (!exist32bit && needInstall32bit, is64BitSystem && !exist64bit && needInstall64bit);
                    }
                case Depend.VC2015to2022:
                    {
                        var exist32bit = RegistryHelper.GetRegistry(@"Installer\Dependencies\VC,redist.x86,x86,14.30,bundle", "Version", RegistryHelper.RegisterType.classesRoot) != null;
                        var exist64bit = RegistryHelper.GetRegistry(@"Installer\Dependencies\VC,redist.x64,amd64,14.30,bundle", "Version", RegistryHelper.RegisterType.classesRoot) != null;
                        return (!exist32bit, is64BitSystem && !exist64bit);
                    }
                case Depend.DirectX9 :
                    {
                        string sysDirPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                        for (int i = 24; i <= 43; i++)
                        {
                            if (!File.Exists(Path.Combine(sysDirPath, $"D3DX9_{i}.dll")) && !File.Exists(Path.Combine(sysDirPath, $"d3dx9_{i}.dll")))
                            {
                                return (true, true);
                            }
                        }
                        return (false, false);
                    }
                case Depend.ParadoxLauncher:
                    {
                        object value = RegistryHelper.GetRegistry(@"SOFTWARE\Paradox Interactive\Paradox Launcher v2", "LauncherInstallation", RegistryHelper.RegisterType.currentUser);
                        if (value != null && File.Exists(Path.Combine((string)value, "bootstrapper-v2.exe")))
                        {
                            return (false, false);
                        }
                        return (true, true);
                    }
                case Depend.ParadoxLauncherCrack: return (true, true);
                default: return(false, false);
            }
        }

        public static void Install(Depend depend, string dependsPath)
        {
            switch (depend)
            {
                case Depend.NetFramwork4_8:
                    {
                        var exePath = Path.Combine(dependsPath, "NetFramwork4_8.exe");
                        ProcessHelper.StartProcess(exePath, "/norestart /q");
                        break;
                    }
                case Depend.VC2005:
                    {
                        var (needInstall32bit, needInstall64bit)= NeedInstall(depend);
                        if (needInstall32bit)
                        {
                            var exePath = Path.Combine(dependsPath, "VC2005x32.exe");
                            ProcessHelper.StartProcess(exePath, "/Q");
                        }
                        if (needInstall64bit)
                        {
                            var exePath = Path.Combine(dependsPath, "VC2005x64.exe");
                            ProcessHelper.StartProcess(exePath, "/Q");
                        }
                        break;
                    }
                case Depend.VC2008:
                    {
                        var (needInstall32bit, needInstall64bit)= NeedInstall(depend);
                        if (needInstall32bit)
                        {
                            var exePath = Path.Combine(dependsPath, "VC2008x32.exe");
                            ProcessHelper.StartProcess(exePath, "/q");
                        }
                        if (needInstall64bit)
                        {
                            var exePath = Path.Combine(dependsPath, "VC2008x64.exe");
                            ProcessHelper.StartProcess(exePath, "/q");
                        }
                        break;
                    }
                case Depend.VC2010:
                    {
                        var (needInstall32bit, needInstall64bit)= NeedInstall(depend);
                        if (needInstall32bit)
                        {
                            var exePath = Path.Combine(dependsPath, "VC2010x32.exe");
                            ProcessHelper.StartProcess(exePath, "/norestart /q");
                        }
                        if (needInstall64bit)
                        {
                            var exePath = Path.Combine(dependsPath, "VC2010x64.exe");
                            ProcessHelper.StartProcess(exePath, "/norestart /q");
                        }
                        break;
                    }
                case Depend.VC2012:
                    {
                        var (needInstall32bit, needInstall64bit)= NeedInstall(depend);
                        if (needInstall32bit)
                        {
                            var exePath = Path.Combine(dependsPath, "VC2012x32.exe");
                            ProcessHelper.StartProcess(exePath, "/norestart /quiet");
                        }
                        if (needInstall64bit)
                        {
                            var exePath = Path.Combine(dependsPath, "VC2012x64.exe");
                            ProcessHelper.StartProcess(exePath, "/norestart /quiet");
                        }
                        break;
                    }
                case Depend.VC2013:
                    {
                        var (needInstall32bit, needInstall64bit)= NeedInstall(depend);
                        if (needInstall32bit)
                        {
                            var exePath = Path.Combine(dependsPath, "VC2013x32.exe");
                            ProcessHelper.StartProcess(exePath, "/norestart /quiet");
                        }
                        if (needInstall64bit)
                        {
                            var exePath = Path.Combine(dependsPath, "VC2013x64.exe");
                            ProcessHelper.StartProcess(exePath, "/norestart /quiet");
                        }
                        break;
                    }
                case Depend.VC2015:
                case Depend.VC2017:
                case Depend.VC2015to2022:
                    {
                        var (needInstall32bit, needInstall64bit)= NeedInstall(depend);
                        if (needInstall32bit)
                        {
                            var exePath = Path.Combine(dependsPath, "VC2015-2022x32.exe");
                            ProcessHelper.StartProcess(exePath, "/norestart /quiet");
                        }
                        if (needInstall64bit)
                        {
                            var exePath = Path.Combine(dependsPath, "VC2015-2022x64.exe");
                            ProcessHelper.StartProcess(exePath, "/norestart /quiet");
                        }
                        break;
                    }
                case Depend.DirectX9:
                    {
                        var exePath = Path.Combine(dependsPath, "DirectX 9", "DXSETUP.exe");
                        ProcessHelper.StartProcess(exePath, "/silent");
                        break;
                    }
                case Depend.ParadoxLauncher:
                    {
                        var exePath = Path.Combine(dependsPath, "ParadoxLauncherInstaller.msi");
                        //ProcessHelper.StartProcess("msiexec.exe", $"/uninstall \"{exePath}\" /quiet /norestart");
                        ProcessHelper.StartProcess("msiexec.exe", $"/i \"{exePath}\" /quiet /norestart");
                        break;
                    }
                case Depend.ParadoxLauncherCrack:
                    {
                        var paradoxLauncherPath = (string)RegistryHelper.GetRegistry(@"SOFTWARE\Paradox Interactive\Paradox Launcher v2", "LauncherInstallation", RegistryHelper.RegisterType.currentUser);
                        var launcherDir = new DirectoryInfo(paradoxLauncherPath);
                        foreach (var childDir in launcherDir.GetDirectories())
                        {
                            string mainDirPath = Path.Combine(childDir.FullName, "resources", "app.asar.unpacked", "dist", "main");
                            if (childDir.Name.Contains("launcher-v2") && Directory.Exists(mainDirPath))
                            {
                                var crackDirPath = Path.Combine(dependsPath, "ParadoxLauncherCrack");
                                FileHelper.CopyFilesRecursively(crackDirPath, mainDirPath);
                            }
                        }
                        break;
                    }
                default: break;
            };
        }
    }
}
