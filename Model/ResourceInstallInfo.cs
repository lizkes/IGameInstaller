using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace IGameInstaller.Model
{
    public enum ResourceType {
        Game = 1,
        Expansion = 2,
        Other = 3,
    }

    public class ResourceInstallInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string EnglishName { get; set; }
        public ResourceType Type { get; set; }
        public int AllowedExp { get; set; }
        public int NormalDownloadCost { get; set; }
        public int FastDownloadCost { get; set; }

        public bool CanNormalDownload { get; set; }
        public bool CanFastDownload { get; set; }
        public string PostInstallPrompt { get; set; }
        public string AfterInstallPrompt { get; set; }
        public Depend[] RequireDepends { get; set; }
        public SystemVersion[] RequireSystems { get; set; }
        public long RequireDisk { get; set; }
        public string ExePath { get; set; }
        public string WorkDirPath { get; set; }
        public string IconPath { get; set; }
        public string ShortCutArgument { get; set; }
        public string ImmutableInstallPath { get; set; }
        public string[] EnsureFilePaths { get; set; }
        public string InstallInputLabel { get; set; }
        public string Md5 { get; set; }

        public static ResourceInstallInfo FromJsonString(string jsonString)
        {
            return JsonConvert.DeserializeObject<ResourceInstallInfo>(jsonString, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
            });
        }
    }
}
