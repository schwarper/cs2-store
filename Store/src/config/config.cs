using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace Store;

public class StoreConfig : BasePluginConfig
{
    public class Config_Database
    {
        public string Host { get; set; } = string.Empty;
        public uint Port { get; set; } = 3306;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class Config_Command
    {
        public string[] Credits { get; set; } = ["credits", "tl"];
        public string[] Store { get; set; } = ["store", "shop", "market"];
        public string[] Inventory { get; set; } = ["inv", "inventory"];
        public string[] GiveCredits { get; set; } = ["givecredits"];
        public string[] Gift { get; set; } = ["gift"];
        public string[] ResetPlayer { get; set; } = ["resetplayer"];
        public string[] ResetDatabase { get; set; } = ["resetdatabase"];
    }

    public class Config_DefaultModel
    {
        public string[] CT { get; set; } = ["characters/models/ctm_fbi/ctm_fbi_variantb.vmdl"];
        public string[] T { get; set; } = ["characters/models/tm_leet/tm_leet_variantj.vmdl"];
    }

    public class Config_Credit
    {
        public int IgnoreWarmup { get; set; } = 1;
        public int Start { get; set; } = 0;
        public int IntervalActiveInActive { get; set; } = 60;
        public int AmountActive { get; set; } = 10;
        public int AmountInActive { get; set; } = 1;
        public int AmountKill { get; set; } = 1;
    }

    public class Config_Menu
    {
        public bool EnableSelling { get; set; } = true;
        public string VipFlag { get; set; } = "@css/root";
        public bool UseWASDMenu { get; set; } = true;
    }

    public class Config_Setting
    {
        public int MaxHealth { get; set; } = 0;
        public int MaxArmor { get; set; } = 0;
        public float SellRatio { get; set; } = 0.60f;
        public bool SellUsePurchaseCredit { get; set; } = false;
        public bool DefaultModelDisableLeg { get; set; } = false;
        public string DatabaseEquipTableName { get; set; } = "store_equipments";
        public float ApplyPlayerskinDelay { get; set; } = 0.60f;
        public string Model0Model1Flag { get; set; } = "@css/root";
    }

    [JsonPropertyName("TagPrefix")] public string Tag { get; set; } = "{red} [Store]";
    [JsonPropertyName("Database")] public Config_Database Database { get; set; } = new Config_Database();
    [JsonPropertyName("Commands")] public Config_Command Commands { get; set; } = new Config_Command();
    [JsonPropertyName("DefaultModels")] public Config_DefaultModel DefaultModels { get; set; } = new Config_DefaultModel();
    [JsonPropertyName("Credits")] public Config_Credit Credits { get; set; } = new Config_Credit();
    [JsonPropertyName("Menu")] public Config_Menu Menu { get; set; } = new Config_Menu();
    [JsonPropertyName("Settings")] public Config_Setting Settings { get; set; } = new Config_Setting();
    [JsonPropertyName("Items")] public Dictionary<string, Dictionary<string, Dictionary<string, string>>> Items { get; set; } = [];
}