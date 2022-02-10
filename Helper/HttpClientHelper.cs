using NLog;
using Polly;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Diagnostics;
using System.Security.Authentication;

using ZstdNet;
using ICSharpCode.SharpZipLib.Tar;
using IGameInstaller.Model;
using IGameInstaller.IGameException;

namespace IGameInstaller.Helper
{
    public class HttpClientHelper
    {
        public readonly static HttpClient httpClient;

        static HttpClientHelper()
        {

            HttpRetryMessageHandler handler = new(
                new HttpClientHandler{
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    MaxConnectionsPerServer = 10,
                    SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,
                    UseProxy = false,
                    Proxy = null,
                }
            );

            httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(8)};
        }

        public static HttpRequestMessage GetIGameApiReq(HttpMethod method, string url)
        {
            var req = new HttpRequestMessage(method, url);
            req.Headers.Add("Accept", "application/json");
            req.Headers.Add("Accept-Encoding", "gzip, deflate");
            req.Headers.Add("User-Agent", $"{App.EnglishName} v{App.Version}");
            return req;
        }

        public static HttpRequestMessage GetOnedriveReq(HttpMethod method, string url)
        {
            var req = new HttpRequestMessage(method, url);
            req.Headers.Add("Accept", "*/*");
            req.Headers.Add("Accept-Encoding", "gzip, deflate");
            req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.131 Safari/537.36");
            return req;
        }

        public static async Task<bool> CheckNetworkAsync()
        {
            try
            {
                await httpClient.GetAsync("http://www.baidu.com");
                return true;
            } 
            catch
            {
                return false;
            }
        }

        public static async Task DownloadFileAsync(string downloadUrl, string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Create);
            var req = GetOnedriveReq(HttpMethod.Get, downloadUrl);
            var resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();
            await resp.Content.CopyToAsync(fs);
        }

        public static void DownloadExtractTask(string downloadUrl, string destDirPath, IProgress<(string, string, int)> progress, CancellationToken token, string promptString = "正在下载并解压缩文件")
        {
            var req = GetOnedriveReq(HttpMethod.Get, downloadUrl);
            var resp = httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).Result;
            resp.EnsureSuccessStatusCode();
            var totalBytes = resp.Content.Headers.ContentLength;
            Directory.CreateDirectory(destDirPath);
            using var downloadStream = resp.Content.ReadAsStreamAsync().Result;

            if (totalBytes.HasValue)
            {
                using var hookStream = new HookStream(downloadStream, (long)totalBytes, progress, token);
                using var zstdStream = new DecompressionStream(hookStream);
                using var tarStream = new TarInputStream(zstdStream, Encoding.UTF8);
                TarEntry tarEntry;
                var sw = Stopwatch.StartNew();
                while ((tarEntry = tarStream.GetNextEntry()) != null)
                {
                    if (tarEntry.IsDirectory)
                        continue;

                    string entryName = tarEntry.Name;
                    if (entryName.StartsWith("./"))
                    {
                        entryName = entryName.Substring(2);
                    }

                    // Converts the unix forward slashes in the filenames to windows backslashes
                    entryName = entryName.Replace('/', Path.DirectorySeparatorChar);

                    // Remove any root e.g. '\' because a PathRooted filename defeats Path.Combine
                    if (Path.IsPathRooted(entryName))
                        entryName = entryName.Substring(Path.GetPathRoot(entryName).Length);

                    // 定时上报
                    if (sw.ElapsedMilliseconds > 200)
                    {
                        progress.Report(($"{promptString}：{entryName}", "keep", -2));
                        sw.Restart();
                    }

                    // Apply further name transformations here as necessary
                    string destPath = Path.Combine(destDirPath, entryName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));

                    var outStream = new FileStream(destPath, FileMode.Create);
                    tarStream.CopyEntryContents(outStream);
                    outStream.Dispose();

                    if (token.IsCancellationRequested)
                    {
                        throw new DownloadExtractCanceledException();
                    }

                    var myDt = DateTime.SpecifyKind(tarEntry.ModTime, DateTimeKind.Utc);
                    File.SetLastWriteTime(destPath, myDt);
                }
            }
            else
            {
                progress.Report(($"{promptString}...", "", -1));
                using var hookStream = new HookStream(downloadStream, token);
                using var zstdStream = new DecompressionStream(hookStream);
                using var tarStream = new TarInputStream(zstdStream, Encoding.UTF8);
                TarEntry tarEntry;
                while ((tarEntry = tarStream.GetNextEntry()) != null)
                {
                    if (tarEntry.IsDirectory)
                        continue;

                    string entryName = tarEntry.Name;
                    if (entryName.StartsWith("./"))
                    {
                        entryName = entryName.Substring(2);
                    }

                    // Converts the unix forward slashes in the filenames to windows backslashes
                    entryName = entryName.Replace('/', Path.DirectorySeparatorChar);

                    // Remove any root e.g. '\' because a PathRooted filename defeats Path.Combine
                    if (Path.IsPathRooted(entryName))
                        entryName = entryName.Substring(Path.GetPathRoot(entryName).Length);

                    // Apply further name transformations here as necessary
                    string destPath = Path.Combine(destDirPath, entryName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));

                    var outStream = new FileStream(destPath, FileMode.Create);
                    tarStream.CopyEntryContents(outStream);
                    outStream.Dispose();

                    if (token.IsCancellationRequested)
                    {
                        throw new DownloadExtractCanceledException();
                    }

                    var myDt = DateTime.SpecifyKind(tarEntry.ModTime, DateTimeKind.Utc);
                    File.SetLastWriteTime(destPath, myDt);
                }
            }
        }
    }

    public class HttpRetryMessageHandler : DelegatingHandler
    {
        public HttpRetryMessageHandler(HttpClientHandler handler) : base(handler) { }
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Policy.Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.2, retryAttempt)), onRetry: (exception, sleepDuration, attemptNumber, context) =>
                {
                    logger.Debug($"http请求失败，{sleepDuration}后重试，重试次数：{attemptNumber} / 5");
                })
                .ExecuteAsync(() => base.SendAsync(request, cancellationToken));
        }
    }
}
