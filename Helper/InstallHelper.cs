using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Net.Http;
using ZstdNet;
using SharpCompress.Readers.Tar;
using SharpCompress.Readers;
using SharpCompress.Common;
using CSScriptLibrary;

using IGameInstaller.Model;

namespace IGameInstaller.Helper
{
    public class InstallHelper
    {
        private static void DownloadExtractTask(string downloadUrl, string destDirPath, IProgress<(string, string, int)> progress, CancellationToken token, string promptString = "正在下载并解压缩文件")
        {
            var req = HttpClientHelper.GetOnedriveReq(HttpMethod.Get, downloadUrl);
            var resp = HttpClientHelper.httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).Result;
            resp.EnsureSuccessStatusCode();
            var totalBytes = resp.Content.Headers.ContentLength;
            Directory.CreateDirectory(destDirPath);
            using var downloadStream = resp.Content.ReadAsStreamAsync().Result;


            if (totalBytes.HasValue)
            {
                using var hookStream = new HookStream(downloadStream, (long)totalBytes, progress, token);
                using var zstdStream = new DecompressionStream(hookStream);
                using var tarReader = TarReader.Open(zstdStream);
                while (tarReader.MoveToNextEntry())
                {
                    if (!tarReader.Entry.IsDirectory)
                    {
                        progress.Report(($"{promptString}：{tarReader.Entry.Key}", "keep", -2));
                        tarReader.WriteEntryToDirectory(destDirPath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                        if (token.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    }
                }
            }
            else
            {
                using var hookStream = new HookStream(downloadStream, token);
                using var zstdStream = new DecompressionStream(hookStream);
                using var tarReader = TarReader.Open(zstdStream);
                progress.Report(($"{promptString}...", "", -1));
                while (tarReader.MoveToNextEntry())
                {
                    if (!tarReader.Entry.IsDirectory)
                    {
                        tarReader.WriteEntryToDirectory(destDirPath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                        if (token.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }
                    }
                }
            }
        }

        public static async Task InstallMainAsync(string downloadUrl, string destDirPath, CancellationToken token)
        {
            var deProgress = new Progress<(string, string, int)>(o => WebSendMessage.SendSetProgress(o.Item1, o.Item2, o.Item3));
            var typeString = App.ResourceInstallInfo.Type == ResourceType.Game ? "游戏" : "拓展";
            await Task.Run(() =>  DownloadExtractTask(downloadUrl, destDirPath, deProgress, token, $"正在下载并解压缩{typeString}文件"), token).ContinueWith((t) =>
            {
                if (t.IsFaulted) throw t.Exception;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static async Task InstallDependsAsync(Depend[] depends, CancellationToken token)
        {
            void InstallDependsTask(IProgress<(string, string, int)> progress)
            {
                for (int i = 0; i < depends.Length; i++)
                {
                    var depend = depends[i];
                    var (needInstall32bit, needInstall64bit) = DependHelper.NeedInstall(depend);
                    if (needInstall32bit || needInstall64bit)
                    {
                        var readableString = DependHelper.DependToReadable(depend);
                        var dependResourceId = DependHelper.DependToResourcId(depend);
                        if (dependResourceId == -1)
                        {
                            throw new Exception($"运行环境没有被收录，请联系作者进行修复：{readableString}");
                        }
                        progress.Report(($"【{i + 1} / {depends.Length}】正在获取运行环境的下载链接: {readableString}", "", -1));
                        var dependDownloadUrl = IGameApiHelper.GetResourceDownloadUrl(dependResourceId).Result;
                        var tempDir = FileHelper.CreateRandomNameDir(Path.GetTempPath());
                        DownloadExtractTask(dependDownloadUrl, tempDir, progress, token, $"【{i + 1} / {depends.Length}】正在下载并解压缩运行环境");
                        progress.Report(($"【{i + 1} / {depends.Length}】正在安装运行环境：{readableString}...", "", -1));
                        DependHelper.Install(depend, tempDir);
                        progress.Report(($"【{i + 1} / {depends.Length}】正在清理多余文件...", "", -1));
                        FileHelper.DeleteEvenWhenUsed(tempDir);
                    }
                }
            }

            var deProgress = new Progress<(string, string, int)>(o => WebSendMessage.SendSetProgress(o.Item1, o.Item2, o.Item3));
            await Task.Run(() => InstallDependsTask(deProgress), token).ContinueWith((t) =>
            {
                if (t.IsFaulted) throw t.Exception;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static async Task ExecuteScriptAsync(string scriptFilePath)
        {
            void ExecuteScriptTask(IProgress<string> progress)
            {
                CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Mono;
                dynamic script = CSScript.Evaluator.LoadCode(File.ReadAllText(scriptFilePath));
                script.Install(progress);
            }

            var executeProgress = new Progress<string>(s => WebSendMessage.SendSetProgress(s, "", -1));
            await Task.Run(() => ExecuteScriptTask(executeProgress)).ContinueWith((t) =>
            {
                if (t.IsFaulted) throw t.Exception;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static async Task FinalWork()
        {
            // 创建快捷方式
            var shortcutName = App.ResourceInstallInfo.Name;
            var exePath = Path.Combine(App.InstallConfig.InstallPath, App.ResourceInstallInfo.ExePath);
            var iconPath = "";
            if (App.ResourceInstallInfo.IconPath != null && App.ResourceInstallInfo.IconPath != "")
            {
                iconPath = Path.Combine(App.InstallConfig.InstallPath, App.ResourceInstallInfo.IconPath);
            }
            var workingDir = Path.GetDirectoryName(exePath);
            if (App.ResourceInstallInfo.WorkDirPath != null && App.ResourceInstallInfo.WorkDirPath != "")
            {
                workingDir = Path.Combine(App.InstallConfig.InstallPath, App.ResourceInstallInfo.WorkDirPath);
            }
            var arguments = App.ResourceInstallInfo.ShortCutArgument;
            if (App.InstallConfig.NeedDesktopShortcut)
            {
                string desktopShortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{shortcutName}.lnk");
                IShellLink link = (IShellLink)new ShellLink();
                link.SetPath(exePath);
                if (iconPath != "")
                {
                    link.SetIconLocation(iconPath, 0);
                }
                if (workingDir != "")
                {
                    link.SetWorkingDirectory(workingDir);
                }
                if (arguments != null && arguments != "")
                {
                    link.SetArguments(arguments);
                }
                IPersistFile file = (IPersistFile)link;
                file.Save(desktopShortcutPath, false);
            }
            if (App.InstallConfig.NeedStartmenuShortcut)
            {
                string startMenuAppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", App.ProjectName);
                Directory.CreateDirectory(startMenuAppPath);
                string startMenuShortcutPath = Path.Combine(startMenuAppPath, $@"{shortcutName}.lnk");
                IShellLink link = (IShellLink)new ShellLink();
                link.SetPath(exePath);
                if (iconPath != "")
                {
                    link.SetIconLocation(iconPath, 0);
                }
                if (workingDir != "")
                {
                    link.SetWorkingDirectory(workingDir);
                }
                if (arguments != null && arguments != "")
                {
                    link.SetArguments(arguments);
                }
                IPersistFile file = (IPersistFile)link;
                file.Save(startMenuShortcutPath, false);
            }

            // 设置注册表
            RegistryHelper.SetResourceRegistry(App.ResourceInstallInfo.Id.ToString(), "InstallPath", App.InstallConfig.InstallPath);

            // 清理安装文件
            await FileHelper.DeleteEvenWhenUsedAsync(Path.Combine(App.InstallConfig.InstallPath, "IGame"));
        }
    }
}
