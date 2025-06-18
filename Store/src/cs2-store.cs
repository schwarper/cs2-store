using System.Reflection;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CS2MenuManager.API.Class;
using Store.Extension;
using StoreApi;
using static StoreApi.Store;

namespace Store;

public class Store : BasePlugin, IPluginConfig<ItemConfig>
{
    public override string ModuleName => "Store";
    public override string ModuleVersion => "v17";
    public override string ModuleAuthor => "schwarper";

    public ItemConfig Config { get; set; } = new();
    public List<StorePlayer> GlobalStorePlayers { get; set; } = [];
    public List<StoreItem> GlobalStorePlayerItems { get; set; } = [];
    public List<StoreEquipment> GlobalStorePlayerEquipments { get; set; } = [];
    public Dictionary<CCSPlayerController, PlayerTimer> GlobalDictionaryPlayer { get; set; } = [];
    public int GlobalTickrate { get; set; }
    public static Store Instance { get; private set; } = new();
    public Random Random { get; set; } = new();
    public Dictionary<CCSPlayerController, float> GlobalGiftTimeout { get; set; } = [];
    public static StoreApi Api { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> Items { get; private set; } = [];
    public Dictionary<CBaseModelEntity, CCSPlayerController> InspectList { get; set; } = [];

    public override void Load(bool hotReload)
    {
        Capabilities.RegisterPluginCapability(IStoreApi.Capability, () => Api);
        Instance = this;

        Event.Load();
        Command.Load();

        if (hotReload)
        {
            var players = Utilities.GetPlayers();
            foreach (CCSPlayerController player in players)
            {
                if (player.IsBot)
                    continue;

                Database.LoadPlayer(player);
                MenuManager.CloseActiveMenu(player);
            }
        }
    }

    public override void Unload(bool hotReload)
    {
        Event.Unload();
        ItemTags.OnPluginEnd();

        var players = Utilities.GetPlayers();
        foreach (CCSPlayerController player in players)
        {
            if (player.IsBot)
                continue;

            MenuManager.CloseActiveMenu(player);
        }
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        ItemModuleManager.RegisterModules(Assembly.GetExecutingAssembly());
    }

    public void OnConfigParsed(ItemConfig config)
    {
        ConfigConfig.Load();

        if (!config.Items.ValueKind.IsValueKindObject())
            throw new JsonException();

        Items = config.Items.ExtractItems();
        Config = config;
    }
}