using IGameInstaller.Helper;
using System;

public class InstallScript
{
    public void Install(IProgress<string> progress)
    {
        // 安装汉化Mod
        progress.Report("正在执行脚本：安装欧陆风云4 1.30.6 汉化Mod...");
        var chineseModPath = ScriptHelper.CombinePath(ScriptHelper.GetInstallPath(), "IGame", "ChineseMod");
        var paradoxModPath = ScriptHelper.CombinePath(ScriptHelper.GetDocumentLocation(), "Paradox Interactive", "Europa Universalis IV", "mod");
        ScriptHelper.MoveFilesRecursively(chineseModPath, paradoxModPath);

        var dlcLoadFile = ScriptHelper.CombinePath(ScriptHelper.GetInstallPath(), "IGame", "dlc_load.json");
        var paradoxPath = ScriptHelper.CombinePath(ScriptHelper.GetDocumentLocation(), "Paradox Interactive", "Europa Universalis IV");
        ScriptHelper.CopyFile(dlcLoadFile, paradoxPath);
    }
}
