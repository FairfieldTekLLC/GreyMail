using System.Reflection;
using Newtonsoft.Json;

namespace EmailReader;

public class Config
{
    [System.Text.Json.Serialization.JsonIgnore]
    private static readonly Lazy<Config> LazyInstance =
        new(CreateInstanceOfT, LazyThreadSafetyMode.ExecutionAndPublication);

    private Config()
    {
    }

    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }

    public string OllamaServerUrl { get; set; }
    public string Model { get; set; }

    public string UserPrincipleName { get; set; }

    public string PromotionsMailFolder { get; set; }


    [Newtonsoft.Json.JsonIgnore] public static Config Instance => LazyInstance.Value;

    private static Config CreateInstanceOfT()
    {
        return Activator.CreateInstance(typeof(Config), true) as Config;
    }

    public void Load()
    {
        string location =
            Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                "Config-Debug.json");

        if (!File.Exists(location))
        {
            location =
                Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                    "Config.json");
        }

        if (!File.Exists(location))
            return;

        string jsonString = File.ReadAllText(location);
        Config config = JsonConvert.DeserializeObject<Config>(jsonString);
        TenantId = config.TenantId;
        ClientId = config.ClientId;
        ClientSecret = config.ClientSecret;
        Model = config.Model;
        OllamaServerUrl = config.OllamaServerUrl;
        UserPrincipleName = config.UserPrincipleName;
        PromotionsMailFolder = config.PromotionsMailFolder;
    }
}