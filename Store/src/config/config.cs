using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Store.Extension;
using System.Reflection;
using System.Text.Json;
using CounterStrikeSharp.API.Core.Translations;
using Tomlyn;
using Tomlyn.Model;

namespace Store;

public class Item_Config : BasePluginConfig
{
    public JsonElement Items { get; set; } = new();
}

public static class Config_Config
{
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

    public static Cfg Config { get; } = new();

    public static void Load()
    {
        if (!File.Exists(ConfigPath))
            throw new FileNotFoundException($"Configuration file not found: {ConfigPath}");

        LoadConfig(ConfigPath);
        Task.Run(async () => await Database.CreateDatabaseAsync(Config.DatabaseConnection));
    }

    private static void LoadConfig(string configPath)
    {
        string configText = File.ReadAllText(configPath);
        TomlTable model = Toml.ToModel(configText);

        Config.DatabaseConnection = model.GetSection<Config_DatabaseConnection>("DatabaseConnection") ?? new();
        Config.Commands = model.GetSection<Config_Commands>("Commands") ?? new();
        Config.DefaultModels = model.GetSection<Config_DefaultModels>("DefaultModels") ?? new();
        Config.Menu = model.GetSection<Config_Menu>("Menu") ?? new();
        Config.Settings = model.GetSection<Config_Settings>("Settings") ?? new();
        Config.Permissions = model.GetSection<Config_Permissions>("Permissions") ?? new();
        Config.Credits = model.TryGetValue("Credits", out object creditsObj) && creditsObj is TomlTable creditsTable
            ? creditsTable.ToDictionary(
                kv => kv.Key,
                kv => kv.Value is TomlTable creditTable ? creditTable.MapTomlTableToObject<Config_Credits>() : new(),
                StringComparer.OrdinalIgnoreCase
            )
            : [];

        Config.Settings.Tag = Config.Settings.Tag.ReplaceColorTags();
    }
}

public sealed class Cfg
{
    public Config_DatabaseConnection DatabaseConnection { get; set; } = new();
    public Config_Commands Commands { get; set; } = new();
    public Config_DefaultModels DefaultModels { get; set; } = new();
    public Dictionary<string, Config_Credits> Credits { get; set; } = [];
    public Config_Menu Menu { get; set; } = new();
    public Config_Settings Settings { get; set; } = new();
    public Config_Permissions Permissions { get; set; } = new();
}

public sealed class Config_DatabaseConnection
{
    public string Host { get; set; } = string.Empty;
    public uint Port { get; set; } = 3306;
    public string User { get; set; } = string.Empty;
    public string Pass { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StorePlayersName { get; set; } = string.Empty;
    public string StoreItemsName { get; set; } = string.Empty;
    public string StoreEquipments { get; set; } = string.Empty;
}

public sealed class Config_Commands
{
    public List<string> Credits { get; set; } = [];
    public List<string> Store { get; set; } = [];
    public List<string> Inventory { get; set; } = [];
    public List<string> GiveCredits { get; set; } = [];
    public List<string> Gift { get; set; } = [];
    public List<string> ResetPlayer { get; set; } = [];
    public List<string> ResetDatabase { get; set; } = [];
    public List<string> RefreshPlayersCredits { get; set; } = [];
    public List<string> HideTrails { get; set; } = [];
    public List<string> PlayerSkinsOff { get; set; } = [];
    public List<string> PlayerSkinsOn { get; set; } = [];
}

public sealed class Config_DefaultModels
{
    public List<string> Terrorist { get; set; } = [];
    public List<string> CounterTerrorist { get; set; } = [];
    public bool DefaultModelDisableLeg { get; set; }
}

public sealed class Config_Credits
{
    public int Start { get; set; }
    public bool IgnoreWarmup { get; set; }
    public int IntervalActiveInActive { get; set; }
    public int AmountActive { get; set; }
    public int AmountInActive { get; set; }
    public int AmountKill { get; set; }
}

public sealed class Config_Menu
{
    public bool EnableSelling { get; set; } = true;
    public bool EnableConfirmMenu { get; set; }
    public string MenuType { get; set; } = string.Empty;
    public string VipFlag { get; set; } = string.Empty;
    public string MenuPressSoundYes { get; set; } = string.Empty;
    public string MenuPressSoundNo { get; set; } = string.Empty;
    public bool CloseMenuAfterSelect { get; set; }
}

public sealed class Config_Settings
{
    public string Tag { get; set; } = string.Empty;
    public int MaxHealth { get; set; }
    public int MaxArmor { get; set; }
    public float SellRatio { get; set; }
    public float ApplyPlayerskinDelay { get; set; }
    public bool SellUsePurchaseCredit { get; set; }
    public bool EnableCs2Fixes { get; set; }
}

public sealed class Config_Permissions
{
    public string Model0Model1Flag { get; set; } = string.Empty;
    public string GiveCredits { get; set; } = string.Empty;
}