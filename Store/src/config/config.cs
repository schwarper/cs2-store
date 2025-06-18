using System.Reflection;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using Store.Extension;
using Tomlyn;
using Tomlyn.Model;

namespace Store;

public class ItemConfig : BasePluginConfig
{
    public JsonElement Items { get; set; } = new();
}

public static class ConfigConfig
{
    private static readonly string ConfigPath;

    static ConfigConfig()
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

        Config.DatabaseConnection = model.GetSection<ConfigDatabaseConnection>("DatabaseConnection") ?? new();
        Config.Commands = model.GetSection<ConfigCommands>("Commands") ?? new();
        Config.DefaultModels = model.GetSection<ConfigDefaultModels>("DefaultModels") ?? new();
        Config.Menu = model.GetSection<ConfigMenu>("Menu") ?? new();
        Config.Settings = model.GetSection<ConfigSettings>("Settings") ?? new();
        Config.Permissions = model.GetSection<ConfigPermissions>("Permissions") ?? new();
        Config.Credits = model.TryGetValue("Credits", out object creditsObj) && creditsObj is TomlTable creditsTable
            ? creditsTable.ToDictionary(
                kv => kv.Key,
                kv => kv.Value is TomlTable creditTable ? creditTable.MapTomlTableToObject<ConfigCredits>() : new(),
                StringComparer.OrdinalIgnoreCase
            )
            : [];

        Config.Settings.Tag = Config.Settings.Tag.ReplaceColorTags();
    }
}

public sealed class Cfg
{
    public ConfigDatabaseConnection DatabaseConnection { get; set; } = new();
    public ConfigCommands Commands { get; set; } = new();
    public ConfigDefaultModels DefaultModels { get; set; } = new();
    public Dictionary<string, ConfigCredits> Credits { get; set; } = [];
    public ConfigMenu Menu { get; set; } = new();
    public ConfigSettings Settings { get; set; } = new();
    public ConfigPermissions Permissions { get; set; } = new();
}

public sealed class ConfigDatabaseConnection
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

public sealed class ConfigCommands
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

public sealed class ConfigDefaultModels
{
    public List<string> Terrorist { get; set; } = [];
    public List<string> CounterTerrorist { get; set; } = [];
    public bool DefaultModelDisableLeg { get; set; }
}

public sealed class ConfigCredits
{
    public int Start { get; set; }
    public bool IgnoreWarmup { get; set; }
    public int IntervalActiveInActive { get; set; }
    public int AmountActive { get; set; }
    public int AmountInActive { get; set; }
    public int AmountKill { get; set; }
}

public sealed class ConfigMenu
{
    public bool EnableConfirmMenu { get; set; }
    public string MenuType { get; set; } = string.Empty;
    public string VipFlag { get; set; } = string.Empty;
    public string MenuPressSoundYes { get; set; } = string.Empty;
    public string MenuPressSoundNo { get; set; } = string.Empty;
    public bool CloseMenuAfterSelect { get; set; }
}

public sealed class ConfigSettings
{
    public string Tag { get; set; } = string.Empty;
    public int MaxHealth { get; set; }
    public int MaxArmor { get; set; }
    public float SellRatio { get; set; }
    public float ApplyPlayerSkinDelay { get; set; }
    public bool SellUsePurchaseCredit { get; set; }
    public bool EnableCs2Fixes { get; set; }
}

public sealed class ConfigPermissions
{
    public string Model0Model1Flag { get; set; } = string.Empty;
    public string GiveCredits { get; set; } = string.Empty;
}