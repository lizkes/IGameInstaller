using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace IGameInstaller.Model
{
    public enum WebSendMessageType
    {
        SetError,
        SetPrompt,
        StartUpdate,
        SetResourceInstallInfo,
        PrepareDone,
        SetProgress,
        SetInstallConfig,
        InstallDone,
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class WebSendMessage
    {
        [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
        public WebSendMessageType Type { get; set; }
        public string Payload { get; set; }

        public WebSendMessage(WebSendMessageType type)
        {
            Type = type;
            Payload = "";
        }
        public WebSendMessage(WebSendMessageType type, string payload)
        {
            Type = type;
            Payload = payload;
        }

        public void Send()
        {
            App.WebView.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(this));
        }

        public static void SendSetError(string title, string content)
        {
            new WebSendMessage(WebSendMessageType.SetError, JsonConvert.SerializeObject(new { title, content })).Send();
        }

        public static void SendSetPrompt(string content)
        {
            new WebSendMessage(WebSendMessageType.SetPrompt, content).Send();
        }

        public static void SendStartUpdate()
        {
            new WebSendMessage(WebSendMessageType.StartUpdate).Send();
        }

        public static void SendSetResourceInstallInfo(ResourceInstallInfo info)
        {
            var setting = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
                Converters = new JsonConverter[] { new StringEnumConverter(typeof(CamelCaseNamingStrategy)) },
            };
            new WebSendMessage(WebSendMessageType.SetResourceInstallInfo, JsonConvert.SerializeObject(info, setting)).Send();
        }

        public static void SendPrepareDone()
        {
            new WebSendMessage(WebSendMessageType.PrepareDone).Send();
        }

        public static void SendSetProgress(string topMessage, string bottomMessage, int progress)
        {
            new WebSendMessage(WebSendMessageType.SetProgress, JsonConvert.SerializeObject(new { topMessage, bottomMessage, progress })).Send();
        }

        public static void SendSetInstallConfig(InstallConfig config)
        {
            var setting = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            };
            new WebSendMessage(WebSendMessageType.SetInstallConfig, JsonConvert.SerializeObject(config, setting)).Send();
        }

        public static void SendInstallDone()
        {
            new WebSendMessage(WebSendMessageType.InstallDone).Send();
        }
    }
}
