using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace Store;

public class StoreConfig : BasePluginConfig
{
    public class Config_Database
    {
        public string host = string.Empty;
        public uint port = 3306;
        public string user = string.Empty;
        public string password = string.Empty;
        public string name = string.Empty;
    }

    public class Config_Command
    {
        public string[] credits = ["credits", "tl"];
        public string[] store = ["store", "shop", "market"];
        public string[] inventory = ["inv", "inventory"];
        public string[] givecredits = ["givecredits"];
        public string[] gift = ["gift"];
        public string[] resetplayer = ["resetplayer"];
        public string[] resetdatabase = ["resetdatabase"];
    }

    public class Config_DefaultModel
    {
        public string[] ct = ["characters/models/ctm_fbi/ctm_fbi_variantb.vmdl"];
        public string[] t = ["characters/models/tm_leet/tm_leet_variantj.vmdl"];
    }

    public class Config_Credit
    {
        public int ignore_warmup = 1;
        public int start = 0;
        public int interval_active_inactive = 60;
        public int amount_active = 10;
        public int amount_inactive = 1;
        public int amount_kill = 1;
    }

    public class Config_Menu
    {
        public bool enable_selling = true;
        public string vip_flag = "@css/root";
        public bool use_wasd_menu = true;
    }

    public class Config_Setting
    {
        public int max_health = 0;
        public int max_armor = 0;
        public float sell_ratio = 0.60f;
        public bool sell_use_purchase_credit = false;
        public bool default_model_disable_leg = false;
        public string database_equip_table_name = "store_equipments";
        public float apply_playerskin_delay = 0.60f;
        public string model0_model1_flag = "@css/root";
    }

    [JsonPropertyName("tag_prefix")] public string Tag { get; set; } = "{red} [Store]";
    [JsonPropertyName("database")] public Config_Database Database { get; set; } = new Config_Database();
    [JsonPropertyName("commands")] public Config_Command Commands { get; set; } = new Config_Command();
    [JsonPropertyName("defaultmodels")] public Config_DefaultModel DefaultModels { get; set; } = new Config_DefaultModel();
    [JsonPropertyName("credits")] public Config_Credit Credits { get; set; } = new Config_Credit();
    [JsonPropertyName("menu")] public Config_Menu Menu { get; set; } = new Config_Menu();
    [JsonPropertyName("settings")] public Config_Setting Settings { get; set; } = new Config_Setting();
    [JsonPropertyName("items")] public Dictionary<string, Dictionary<string, Dictionary<string, string>>> Items { get; set; } = [];
}
