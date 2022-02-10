using IGameInstaller.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IGameInstaller.Helper
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class IGameApiError
    {
        public int Code { get; set; }
        public string Cause { get; set; }
        public string Content { get; set; }

        public static IGameApiError FromJsonString(string jsonString)
        {
            return JsonConvert.DeserializeObject<IGameApiError>(jsonString);
        }
    }
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class IGameApiDownloadUrlData
    {
        public string DownloadUrl { get; set; }
        public static IGameApiDownloadUrlData FromJsonString(string jsonString)
        {
            return JsonConvert.DeserializeObject<IGameApiDownloadUrlData>(jsonString);
        }
    }
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class IGameApiVersionData
    {
        public string Version { get; set; }
        public static IGameApiVersionData FromJsonString(string jsonString)
        {
            return JsonConvert.DeserializeObject<IGameApiVersionData>(jsonString);
        }
    }
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class IGameApiErrorPayload
    {
        public string AppName { get; set; }
        public string Content { get;  set; }
        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
    public class IGameApiHelper
    {
        public static string IGameApiUrl { get; } = "https://api.igame.ml";

        public static async Task HandleApiError(HttpResponseMessage resp)
        {
            if (resp.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorString = await resp.Content.ReadAsStringAsync();
                var igameApiError = IGameApiError.FromJsonString(errorString);
                throw new HttpRequestException($"客户端错误\n{igameApiError.Content}");
            }
            else if (resp.StatusCode == HttpStatusCode.InternalServerError)
            {
                var errorString = await resp.Content.ReadAsStringAsync();
                var igameApiError = IGameApiError.FromJsonString(errorString);
                if (igameApiError.Code == 500)
                {
                    throw new HttpRequestException($"服务器维护中\n预计将于 {TimeHelper.DateTimeFormat(TimeHelper.StringToDateTime(igameApiError.Content))} 恢复正常");
                }
                else
                {
                    throw new HttpRequestException($"服务器错误\n{igameApiError.Content}");
                }
            }
            else if (resp.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                throw new HttpRequestException("访问频率过快，请稍后再试");
            }
            else
            {
                resp.EnsureSuccessStatusCode();
            }
        }

        public static async Task<string> GetResourceVersion(int resourceId)
        {
            var req = HttpClientHelper.GetIGameApiReq(HttpMethod.Get, $"{IGameApiUrl}/resource/{resourceId}/version");
            var resp = await HttpClientHelper.httpClient.SendAsync(req, HttpCompletionOption.ResponseContentRead);
            await HandleApiError(resp);
            var jsonString = await resp.Content.ReadAsStringAsync();
            return IGameApiVersionData.FromJsonString(jsonString).Version;
        }

        public static async Task<string> GetResourceDownloadUrl(int resourceId, string providerGroup = "fast")
        {
            var req = HttpClientHelper.GetIGameApiReq(HttpMethod.Get, $"{IGameApiUrl}/resource/{resourceId}/download_url?provider_group={providerGroup}");
            var resp = await HttpClientHelper.httpClient.SendAsync(req, HttpCompletionOption.ResponseContentRead);
            await HandleApiError(resp);
            var jsonString = await resp.Content.ReadAsStringAsync();
            return IGameApiDownloadUrlData.FromJsonString(jsonString).DownloadUrl;
        }

        public static async Task<ResourceInstallInfo> GetResourceInstallInfo(int resourceId)
        {
            var req = HttpClientHelper.GetIGameApiReq(HttpMethod.Get, $"{IGameApiUrl}/resource/{resourceId}/install_info");
            var resp = await HttpClientHelper.httpClient.SendAsync(req, HttpCompletionOption.ResponseContentRead);
            await HandleApiError(resp);
            var jsonString = await resp.Content.ReadAsStringAsync();
            return ResourceInstallInfo.FromJsonString(jsonString);
        }

        public static async Task ErrorCollect(string errorMessage)
        {
            var req = HttpClientHelper.GetIGameApiReq(HttpMethod.Post, $"{IGameApiUrl}/error/collect");
            var encryptedMessage = CryptoHelper.Base64Encode(CryptoHelper.AesEncrypt(errorMessage));
            var errorPayload = new IGameApiErrorPayload { AppName = App.EnglishName, Content = encryptedMessage };
            req.Content = new StringContent(errorPayload.ToJsonString(), Encoding.UTF8, "application/json");
            var resp = await HttpClientHelper.httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            await HandleApiError(resp);
        }
    }
}
