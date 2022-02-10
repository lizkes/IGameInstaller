using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using IGameInstaller.Model;

namespace IGameInstaller.Helper
{
    public class UpdateHelper
    {
        public static async Task<bool> NeedUpdateAsync()
        {
            var remoteVersion = await IGameApiHelper.GetResourceVersion(App.ResourceId);
            if (new Version(remoteVersion) > App.Version) return true;
            return false;
        }

        public static async Task UpdateAsync()
        {
            WebSendMessage.SendSetProgress("正在获取更新链接...", "", -1);
            string downloadUrl = await IGameApiHelper.GetResourceDownloadUrl(App.ResourceId);
            string igameUpdateDir = FileHelper.CreateRandomNameDir(Path.GetTempPath());
            var deProgress = new Progress<(string, string, int)>(o => WebSendMessage.SendSetProgress(o.Item1, o.Item2, o.Item3));
            var token = new CancellationToken(false);
            await Task.Run(() => HttpClientHelper.DownloadExtractTask(downloadUrl, igameUpdateDir, deProgress, token, "正在下载并解压缩更新文件"), token).ContinueWith((t) =>
            {
                if (t.IsFaulted) throw t.Exception;
            }, TaskScheduler.FromCurrentSynchronizationContext());
            string updaterPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "IGameUpdater.exe");
            var args = $"\"{igameUpdateDir}\" \"IGameInstaller.exe\" \"{App.resourceId}\"";
            ProcessHelper.StartProcess(updaterPath, args, false, false, Path.GetDirectoryName(updaterPath));
            Application.Current.Shutdown(0);
        }
    }
}
