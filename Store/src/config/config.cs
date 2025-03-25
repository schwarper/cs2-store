using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CS2MenuManager.API.Menu;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Model;

namespace Store;

public class Item_Config : BasePluginConfig
{
    [JsonPropertyName("Items")] public JsonElement Items { get; set; } = new();
}

public static class Config_Config
{
    public static Cfg Config { get; set; } = new Cfg();

    private static readonly string ConfigPath;

    static Config_Config()
    {
        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;

        ConfigPath = Path.Combine(Server.GameDirectory,
            "csgo",
            "addons",
            "counterstrikesharp",
            "configs",
            "plugins",
            assemblyName,
            "config.toml"
        );
    }

    public static void Load()
    {
        if (!File.Exists(ConfigPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {ConfigPath}");
        }

        LoadConfig(ConfigPath);

        Task.Run(async () =>
        {
            await Database.CreateDatabaseAsync(Config.DatabaseConnection);
        });
    }

    private static void LoadConfig(string configPath)
    {
        string configText = File.ReadAllText(configPath);
        TomlTable model = Toml.ToModel(configText);

        Config = new Cfg
        {
            Tag = StringExtensions.ReplaceColorTags(((TomlTable)model["Tag"])["Tag"].ToString()!),
            DatabaseConnection = LoadDatabaseConnection((TomlTable)model["DatabaseConnection"]),
            Commands = LoadCommands((TomlTable)model["Commands"]),
            DefaultModels = LoadDefaultModels((TomlTable)model["DefaultModels"]),
            Credits = LoadCredits((TomlTable)model["Credits"]),
            Menu = LoadMenu((TomlTable)model["Menu"]),
            Settings = LoadSettings((TomlTable)model["Settings"])
        };
    }

    private static Config_DatabaseConnection LoadDatabaseConnection(TomlTable dbTable)
    {
        Config_DatabaseConnection config = new()
        {
            Host = dbTable["Host"].ToString()!,
            Port = uint.Parse(dbTable["Port"].ToString()!),
            User = dbTable["User"].ToString()!,
            Password = dbTable["Password"].ToString()!,
            Name = dbTable["Name"].ToString()!,
            DatabaseEquipTableName = dbTable["DatabaseEquipTableName"].ToString()!
        };

        return string.IsNullOrEmpty(config.Host) || string.IsNullOrEmpty(config.Name) || string.IsNullOrEmpty(config.User)
            ? throw new Exception("You need to setup Database credentials in config.")
            : config;
    }

    private static Config_Commands LoadCommands(TomlTable commandsTable)
    {
        return new()
        {
            Credits = GetStringArray(commandsTable["Credits"]),
            Store = GetStringArray(commandsTable["Store"]),
            Inventory = GetStringArray(commandsTable["Inventory"]),
            GiveCredits = GetStringArray(commandsTable["GiveCredits"]),
            Gift = GetStringArray(commandsTable["Gift"]),
            ResetPlayer = GetStringArray(commandsTable["ResetPlayer"]),
            ResetDatabase = GetStringArray(commandsTable["ResetDatabase"]),
            RefreshPlayersCredits = GetStringArray(commandsTable["RefreshPlayersCredits"]),
            HideTrails = GetStringArray(commandsTable["HideTrails"]),
            ModelOff = GetStringArray(commandsTable["PlayerSkinsOff"]),
            ModelOn = GetStringArray(commandsTable["PlayerSkinsOn"])
        };
    }

    private static Config_DefaultModels LoadDefaultModels(TomlTable defaultModelsTable)
    {
        return new()
        {
            Terrorist = GetStringArray(defaultModelsTable["Terrorist"]),
            CounterTerrorist = GetStringArray(defaultModelsTable["CounterTerrorist"])
        };
    }

    private static Config_Credits LoadCredits(TomlTable creditsTable)
    {
        return new()
        {
            Start = int.Parse(creditsTable["Start"].ToString()!),
            IntervalActiveInActive = int.Parse(creditsTable["IntervalActiveInActive"].ToString()!),
            AmountActive = int.Parse(creditsTable["AmountActive"].ToString()!),
            AmountInActive = int.Parse(creditsTable["AmountInActive"].ToString()!),
            AmountKill = int.Parse(creditsTable["AmountKill"].ToString()!),
            IgnoreWarmup = bool.Parse(creditsTable["IgnoreWarmup"].ToString()!)
        };
    }

    private static Config_Menu LoadMenu(TomlTable menuTable)
    {
        return new()
        {
            EnableSelling = bool.Parse(menuTable["EnableSelling"].ToString()!),
            EnableConfirmMenu = bool.Parse(menuTable["EnableConfirmMenu"].ToString()!),
            MenuType = menuTable["MenuType"].ToString()!,
            VipFlag = menuTable["VipFlag"].ToString()!,
            MenuPressSoundYes = menuTable["MenuPressSoundYes"].ToString()!,
            MenuPressSoundNo = menuTable["MenuPressSoundNo"].ToString()!
        };
    }

    private static Config_Settings LoadSettings(TomlTable settingsTable)
    {
        return new()
        {
            MaxHealth = int.Parse(settingsTable["MaxHealth"].ToString()!),
            MaxArmor = int.Parse(settingsTable["MaxArmor"].ToString()!),
            SellRatio = float.Parse(settingsTable["SellRatio"].ToString()!),
            ApplyPlayerskinDelay = float.Parse(settingsTable["ApplyPlayerskinDelay"].ToString()!),
            SellUsePurchaseCredit = bool.Parse(settingsTable["SellUsePurchaseCredit"].ToString()!),
            DefaultModelDisableLeg = bool.Parse(settingsTable["DefaultModelDisableLeg"].ToString()!),
            Model0Model1Flag = settingsTable["Model0Model1Flag"].ToString()!
        };
    }

    private static string[] GetStringArray(object tomlArray)
    {
        return [.. ((TomlArray)tomlArray).Select(item => item!.ToString()!)];
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
        public string[] RefreshPlayersCredits { get; set; } = [];
        public string[] HideTrails { get; set; } = [];
        public string[] ModelOff { get; set; } = [];
        public string[] ModelOn { get; set; } = [];
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
        public string MenuType { get; set; } = "worldtext";
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

    public static readonly Dictionary<string, Type> MenuTypes = new()
    {
        { "CenterHtmlMenu", typeof(CenterHtmlMenu) },
        { "ConsoleMenu", typeof(ConsoleMenu) },
        { "ChatMenu", typeof(ChatMenu) },
        { "ScreenMenu", typeof(ScreenMenu) },
        { "WasdMenu", typeof(WasdMenu) }
    };
}