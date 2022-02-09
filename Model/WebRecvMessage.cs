using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace IGameInstaller.Model
{
    public enum WebRecvMessageType
    {
        StartPrepare,
        GenerateInstallConfig,
        GetInstallPath,
        SetInstallConfig,
        StartDownload,
        OpenBrowser,
        OpenGame,
        TaskCancel,
        Exit,
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class WebRecvMessage
    {

        [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public WebRecvMessageType Type { get; set; }
        public string Payload { get; set; }

        public WebRecvMessage(WebRecvMessageType type, string payload)
        {
            Type = type;
            Payload = payload;
        }
        public O DeserializePayload<O>()
        {
            return JsonConvert.DeserializeObject<O>(Payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            });
        }
        public static WebRecvMessage FromJsonString(string jsonString)
        {
            return JsonConvert.DeserializeObject<WebRecvMessage>(jsonString);
        }
    }
}
