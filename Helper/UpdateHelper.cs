using IGameInstaller.Model;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Readers.Tar;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ZstdNet;

namespace IGameInstaller.Helper
{
    public class UpdateHelper
    {
        public static async Task ExtractTzstAsync(string tzstPath, string destDirPath)
        {
            void ExtractTzstTask(IProgress<(string, string, int)> progress)
            {
                var fileSize = new FileInfo(tzstPath).Length;
                using var tzstStream = File.OpenRead(tzstPath);
                using var hookStream = new HookStream(tzstStream, fileSize, progress, new CancellationToken(false));
                using var zstdStream = new DecompressionStream(hookStream);
                using var tarReader = TarReader.Open(zstdStream);

                while (tarReader.MoveToNextEntry())
                {
                    if (!tarReader.Entry.IsDirectory)
                    {
                         progress.Report(($"正在解压缩更新文件：{tarReader.Entry.Key}", "keep", -2));
                        tarReader.WriteEntryToDirectory(destDirPath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }

            var progress = new Progress<(string, string, int)>(o => WebSendMessage.SendSetProgress(o.Item1, o.Item2, o.Item3));
            await Task.Run(() => {
                ExtractTzstTask(progress);
                FileHelper.Retry(() => File.Delete(tzstPath));
            });
        }

        public static async Task DownloadFileAsync(string downloadUrl, string filePath)
        {
            var req = HttpClientHelper.GetOnedriveReq(HttpMethod.Get, downloadUrl);
            var resp = await HttpClientHelper.httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();
            var totalBytes = resp.Content.Headers.ContentLength;

            if (totalBytes.HasValue)
            {
                using var fs = new FileStream(filePath, FileMode.Create);
                using var contentStream = await resp.Content.ReadAsStreamAsync();
                var readedTotalBytes = 0L;
                var buffer = new byte[8192];
                var isMoreToRead = true;
                var beforeProgress = -1;
                do
                {
                    var readedBytes = await contentStream.ReadAsync(buffer, 0, 8192);
                    if (readedBytes == 0)
                    {
                        isMoreToRead = false;
                        continue;
                    }
                    await fs.WriteAsync(buffer, 0, readedBytes);
                    readedTotalBytes += readedBytes;
                    var nowProgress = (int)Math.Round(readedTotalBytes / (double)totalBytes * 100, 0);
                    if (nowProgress > beforeProgress)
                    {
                        beforeProgress = nowProgress;
                        WebSendMessage.SendSetProgress("正在下载更新文件...", $"{nowProgress} %" , nowProgress);
                    }
                }
                while (isMoreToRead);
            } 
            else
            {
                WebSendMessage.SendSetProgress("正在下载更新文件...", "", -1);
                await HttpClientHelper.DownloadFileAsync(downloadUrl, filePath);
            }
        }

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
            string igameTzstPath = Path.Combine(Path.GetTempPath(), "IGameInstaller.tzst");
            await DownloadFileAsync(downloadUrl, igameTzstPath);
            string IgameUpdateDir = FileHelper.CreateRandomNameDir(Path.GetTempPath());
            await ExtractTzstAsync(igameTzstPath, IgameUpdateDir);
            string updaterPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "IGameUpdater.exe");
            var args = $"\"{IgameUpdateDir}\" \"IGameInstaller.exe\" \"{App.resourceId}\"";
            ProcessHelper.StartProcess(updaterPath, args, false, false, Path.GetDirectoryName(updaterPath));
            Application.Current.Shutdown(0);
        }
    }
}
