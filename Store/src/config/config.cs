using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using System.Reflection;
using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Model;

namespace Store;

public class Item_Config : BasePluginConfig
{
    [JsonPropertyName("Items")] public Dictionary<string, Dictionary<string, Dictionary<string, string>>> Items { get; set; } = [];
}

public static class Config_Config
{
    public static Cfg Config { get; set; } = new Cfg();

    public static void Load()
    {
        string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "";
        string CfgPath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/{AssemblyName}";

        LoadConfig($"{CfgPath}/config.toml");

        Task.Run(async () =>
        {
            await Database.CreateDatabaseAsync(Config.DatabaseConnection);
        });
    }

    private static void LoadConfig(string configPath)
    {
        var configText = File.ReadAllText(configPath);
        var model = Toml.ToModel(configText);

        var tagTable = (TomlTable)model["Tag"];
        var config_tag = StringExtensions.ReplaceColorTags(tagTable["Tag"].ToString()!);

        var dbTable = (TomlTable)model["DatabaseConnection"];
        var config_database = new Config_DatabaseConnection
        {
            Host = dbTable["Host"].ToString()!,
            Port = uint.Parse(dbTable["Port"].ToString()!),
            User = dbTable["User"].ToString()!,
            Password = dbTable["Password"].ToString()!,
            Name = dbTable["Name"].ToString()!,
            DatabaseEquipTableName = dbTable["DatabaseEquipTableName"].ToString()!
        };

        if (string.IsNullOrEmpty(config_database.Host) || string.IsNullOrEmpty(config_database.Name) || string.IsNullOrEmpty(config_database.User))
        {
            throw new Exception("You need to setup Database credentials in config.");
        }

        var commandsTable = (TomlTable)model["Commands"];
        var creditsList = new List<string>();
        foreach (var item in (TomlArray)commandsTable["Credits"])
        {
            creditsList.Add(item!.ToString()!);
        }

        var storeList = new List<string>();
        foreach (var item in (TomlArray)commandsTable["Store"])
        {
            storeList.Add(item!.ToString()!);
        }

        var inventoryList = new List<string>();
        foreach (var item in (TomlArray)commandsTable["Inventory"])
        {
            inventoryList.Add(item!.ToString()!);
        }

        var giveCreditsList = new List<string>();
        foreach (var item in (TomlArray)commandsTable["GiveCredits"])
        {
            giveCreditsList.Add(item!.ToString()!);
        }

        var giftList = new List<string>();
        foreach (var item in (TomlArray)commandsTable["Gift"])
        {
            giftList.Add(item!.ToString()!);
        }

        var resetPlayerList = new List<string>();
        foreach (var item in (TomlArray)commandsTable["ResetPlayer"])
        {
            resetPlayerList.Add(item!.ToString()!);
        }

        var resetDatabaseList = new List<string>();
        foreach (var item in (TomlArray)commandsTable["ResetDatabase"])
        {
            resetDatabaseList.Add(item!.ToString()!);
        }

        var config_commands = new Config_Commands
        {
            Credits = [.. creditsList],
            Store = [.. storeList],
            Inventory = [.. inventoryList],
            GiveCredits = [.. giveCreditsList],
            Gift = [.. giftList],
            ResetPlayer = [.. resetPlayerList],
            ResetDatabase = [.. resetDatabaseList]
        };

        var defaultModelsTable = (TomlTable)model["DefaultModels"];
        var terroristList = new List<string>();
        foreach (var item in (TomlArray)defaultModelsTable["Terrorist"])
        {
            terroristList.Add(item!.ToString()!);
        }

        var counterTerroristList = new List<string>();
        foreach (var item in (TomlArray)defaultModelsTable["CounterTerrorist"])
        {
            counterTerroristList.Add(item!.ToString()!);
        }

        var config_defaultModels = new Config_DefaultModels
        {
            Terrorist = [.. terroristList],
            CounterTerrorist = [.. counterTerroristList]
        };

        var creditsTable = (TomlTable)model["Credits"];
        var config_credits = new Config_Credits
        {
            Start = int.Parse(creditsTable["Start"].ToString()!),
            IntervalActiveInActive = int.Parse(creditsTable["IntervalActiveInActive"].ToString()!),
            AmountActive = int.Parse(creditsTable["AmountActive"].ToString()!),
            AmountInActive = int.Parse(creditsTable["AmountInActive"].ToString()!),
            AmountKill = int.Parse(creditsTable["AmountKill"].ToString()!),
            IgnoreWarmup = bool.Parse(creditsTable["IgnoreWarmup"].ToString()!)
        };

        var menuTable = (TomlTable)model["Menu"];
        var config_menu = new Config_Menu
        {
            EnableSelling = bool.Parse(menuTable["EnableSelling"].ToString()!),
            EnableConfirmMenu = bool.Parse(menuTable["EnableConfirmMenu"].ToString()!),
            UseWASDMenu = bool.Parse(menuTable["UseWASDMenu"].ToString()!),
            VipFlag = menuTable["VipFlag"].ToString()!,
            MenuPressSoundYes = menuTable["MenuPressSoundYes"].ToString()!,
            MenuPressSoundNo = menuTable["MenuPressSoundNo"].ToString()!
        };

        var settingsTable = (TomlTable)model["Settings"];
        var config_settings = new Config_Settings
        {
            MaxHealth = int.Parse(settingsTable["MaxHealth"].ToString()!),
            MaxArmor = int.Parse(settingsTable["MaxArmor"].ToString()!),
            SellRatio = float.Parse(settingsTable["SellRatio"].ToString()!),
            ApplyPlayerskinDelay = float.Parse(settingsTable["ApplyPlayerskinDelay"].ToString()!),
            SellUsePurchaseCredit = bool.Parse(settingsTable["SellUsePurchaseCredit"].ToString()!),
            DefaultModelDisableLeg = bool.Parse(settingsTable["DefaultModelDisableLeg"].ToString()!),
            Model0Model1Flag = settingsTable["Model0Model1Flag"].ToString()!
        };

        Config = new Cfg
        {
            Tag = config_tag,
            DatabaseConnection = config_database,
            Commands = config_commands,
            DefaultModels = config_defaultModels,
            Credits = config_credits,
            Menu = config_menu,
            Settings = config_settings
        };
    }

    public class Cfg
    {
        public string Tag { get; set; } = "{red}[Store] ";
        public Config_DatabaseConnection DatabaseConnection { get; set; } = new Config_DatabaseConnection();
        public Config_Commands Commands { get; set; } = new Config_Commands();
        public Config_DefaultModels DefaultModels { get; set; } = new Config_DefaultModels();
        public Config_Credits Credits { get; set; } = new Config_Credits();
        public Config_Menu Menu { get; set; } = new Config_Menu();
        public Config_Settings Settings { get; set; } = new Config_Settings();
    }

    public class Config_DatabaseConnection
    {
        public string Host { get; set; } = "";
        public uint Port { get; set; } = 3306;
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public string Name { get; set; } = "";
        public string DatabaseEquipTableName { get; set; } = "store_equipments";
    }

    public class Config_Commands
    {
        public string[] Credits { get; set; } = [];
        public string[] Store { get; set; } = [];
        public string[] Inventory { get; set; } = [];
        public string[] GiveCredits { get; set; } = [];
        public string[] Gift { get; set; } = [];
        public string[] ResetPlayer { get; set; } = [];
        public string[] ResetDatabase { get; set; } = [];
    }

    public class Config_DefaultModels
    {
        public string[] Terrorist { get; set; } = [];
        public string[] CounterTerrorist { get; set; } = [];
    }

    public class Config_Credits
    {
        public int Start { get; set; } = 0;
        public int IntervalActiveInActive { get; set; } = 60;
        public int AmountActive { get; set; } = 10;
        public int AmountInActive { get; set; } = 1;
        public int AmountKill { get; set; } = 1;
        public bool IgnoreWarmup { get; set; } = true;
    }

    public class Config_Menu
    {
        public bool EnableSelling { get; set; } = true;
        public bool EnableConfirmMenu { get; set; } = true;
        public bool UseWASDMenu { get; set; } = true;
        public string VipFlag { get; set; } = "@css/root";
        public string MenuPressSoundYes { get; set; } = "";
        public string MenuPressSoundNo { get; set; } = "";
    }

    public class Config_Settings
    {
        public int MaxHealth { get; set; } = 0;
        public int MaxArmor { get; set; } = 0;
        public float SellRatio { get; set; } = 0.6f;
        public float ApplyPlayerskinDelay { get; set; } = 0.6f;
        public bool SellUsePurchaseCredit { get; set; } = false;
        public bool DefaultModelDisableLeg { get; set; } = false;
        public string Model0Model1Flag { get; set; } = "@css/root";
    }
}