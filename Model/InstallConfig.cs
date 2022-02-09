
using System.IO;
using System.Linq;

using IGameInstaller.Helper;

namespace IGameInstaller.Model
{
    public class InstallConfig
    {
        public string InstallPath { get; set; }
        public bool InstallPathIsImmutable { get; set; }
        public bool NeedDesktopShortcut { get; set; }
        public bool NeedStartmenuShortcut { get; set; }
        public long DiskFreeSpace { get; set; }
        public string InputLabel { get; set; }
        public string SuccessMessage { get; set; }
        public string InfoMessage { get; set; }
        public string ErrorMessage { get; set; }


        public InstallConfig()
        {
            if (App.ResourceInstallInfo.ImmutableInstallPath != null && App.ResourceInstallInfo.ImmutableInstallPath != "")
            {
                var immutableInstallPath = App.ResourceInstallInfo.ImmutableInstallPath;
                if (immutableInstallPath.Contains("[Document]"))
                {
                    InstallPath = immutableInstallPath.Replace("[Document]", ScriptHelper.GetDocumentLocation());
                } else
                {
                    InstallPath = immutableInstallPath;
                }
                InstallPathIsImmutable = true;
                SuccessMessage = "已自动选择正确的安装路径";
            } 
            else
            {
                var installPath = RegistryHelper.GetResourceRegistry(App.ResourceInstallInfo.Id.ToString(), "InstallPath");
                if (installPath != null && Directory.Exists((string)installPath))
                {
                    InstallPath = (string)installPath;
                }
                else if (App.ResourceInstallInfo.Type == ResourceType.Expansion)
                {
                    var driveName = FileHelper.GetMaxFreeSpaceDriver();
                    InstallPath = $@"{driveName}";
                }
                else
                {
                    var driveName = FileHelper.GetMaxFreeSpaceDriver();
                    InstallPath = $@"{driveName}{App.ProjectName}\{App.ResourceInstallInfo.EnglishName}";
                }
                InstallPathIsImmutable = false;
            }

            if (App.ResourceInstallInfo.Type == ResourceType.Game)
            {
                NeedDesktopShortcut = true;
            } 
            else
            {
                NeedDesktopShortcut = false;
            }
            NeedStartmenuShortcut = false;
            DiskFreeSpace = FileHelper.GetDriverFreeSize(InstallPath.Substring(0, 3));

            if (App.ResourceInstallInfo.InstallInputLabel != null && App.ResourceInstallInfo.InstallInputLabel != "")
            {
                InputLabel = App.ResourceInstallInfo.InstallInputLabel;
            }
            else
            {
                InputLabel = App.ResourceInstallInfo.Type == ResourceType.Game ? "游戏安装路径" : "拓展安装路径";
            }

            Verify();
        }

        public void Verify()
        {
            //if install path is exist
            if (Directory.Exists(InstallPath))
            {
                //if this folder is not empty
                if (App.ResourceInstallInfo.Type == ResourceType.Game && Directory.EnumerateFileSystemEntries(InstallPath).Any())
                {
                    InfoMessage = "该文件夹内已有文件，继续安装将导致部分文件被覆盖";
                }
            }
            //if non-ASCII char in install path
            for (int i = 0; i < InstallPath.Length; i++)
            {
                if (InstallPath[i] > 127)
                {
                    ErrorMessage = "安装路径中只能包含英文和部分特殊字符";
                    return;
                }
            }
            //if the driver of install path has not enought space
            if (DiskFreeSpace < App.ResourceInstallInfo.RequireDisk)
            {
                ErrorMessage = $"{InstallPath.Substring(0, 1)}盘的剩余空间不足，请清理磁盘后重试";
                return;
            }
            // 确保需求文件在安装路径内
            if (App.ResourceInstallInfo.EnsureFilePaths != null && App.ResourceInstallInfo.EnsureFilePaths.Length != 0)
            {
                foreach (var ensureFilePath in App.ResourceInstallInfo.EnsureFilePaths)
                {
                    if (!File.Exists(Path.Combine(InstallPath, ensureFilePath)))
                        {
                            ErrorMessage = "安装路径不正确，请选择正确的安装路径";
                        return;
                    }
                }
            }
        }
        public void SetPath(string path)
        {
            InstallPath = path;
            DiskFreeSpace = FileHelper.GetDriverFreeSize(path.Substring(0, 3));
            SuccessMessage = null;
            InfoMessage = null;
            ErrorMessage = null;
            Verify();
        }
    }
}
