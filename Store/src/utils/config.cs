using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace Store;

public class StoreConfig : BasePluginConfig
{
    [JsonPropertyName("database")]
    public Dictionary<string, string> Database { get; set; } = new Dictionary<string, string>() {
        { "host", string.Empty },
        { "port", "3306" },
        { "user", string.Empty },
        { "password", string.Empty },
        { "name", string.Empty }
    };

    [JsonPropertyName("commands")]
    public Dictionary<string, string[]> Commands { get; set; } = new Dictionary<string, string[]>
    {
        { "credits", ["credits", "tl"] },
        { "store", ["store", "shop", "market"] },
        { "inventory", ["inv", "inventory"] },
        { "givecredits", ["givecredits"] },
        { "gift", ["gift"] },
        { "resetplayer", ["resetplayer"] },
        { "resetdatabase", ["resetdatabase"] }
    };

    [JsonPropertyName("defaultmodels")]
    public Dictionary<string, string[]> DefaultModels { get; set; } = new Dictionary<string, string[]>
    {
        { "ct", ["characters/models/ctm_fbi/ctm_fbi_variantb.vmdl" ] },
        { "t", [ "characters/models/tm_leet/tm_leet_variantj.vmdl" ] }
    };

    [JsonPropertyName("credits")]
    public Dictionary<string, int> Credits { get; set; } = new Dictionary<string, int>
    {
        {"ignore_warmup", 1 },
        {"start", 0 },
        {"interval_active_inactive", 60 },
        {"amount_active", 10 },
        {"amount_inactive", 1 },
        {"amount_kill", 1 }
    };

    [JsonPropertyName("menu")]
    public Dictionary<string, string> Menu { get; set; } = new Dictionary<string, string>
    {
        { "enable_selling", "1" },
        { "vip_flag", "@css/root" }
    };

    [JsonPropertyName("settings")]
    public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>
    {
        { "max_health", "0" },
        { "max_armor", "0" },
        { "sell_ratio", "0.60f" },
        { "default_model_disable_leg", "false" },
        { "apply_playerskin_delay", "0.60f" }
    };

    [JsonPropertyName("items")]
    public Dictionary<string, Dictionary<string, Dictionary<string, string>>> Items { get; set; } = [];
}