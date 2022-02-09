using NLog;
using Polly;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                    UseProxy = false,
                    Proxy = null,
                }
            );

            httpClient = new HttpClient(handler);
        }

        public static HttpRequestMessage GetIGameApiReq(HttpMethod method, string url)
        {
            var req = new HttpRequestMessage(method, url);
            req.Headers.Add("Accept", "application/json");
            req.Headers.Add("Accept-Encoding", "gzip, deflate");
            req.Headers.Add("Agent", $"{App.EnglishName} v{App.Version}");
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
                .Or<TimeoutException>()
                .Or<OperationCanceledException>()
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt)), onRetry: (exception, sleepDuration, attemptNumber, context) =>
                {
                    logger.Debug($"http请求失败，{sleepDuration}后重试，重试次数：{attemptNumber} / 5");
                })
                .ExecuteAsync(() => base.SendAsync(request, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token));
        }
    }
}
