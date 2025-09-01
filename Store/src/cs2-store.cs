using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CS2MenuManager.API.Class;
using Store.Extension;
using StoreApi;
using System.Reflection;
using System.Text.Json;
using static StoreApi.Store;

namespace Store;

public class Store : BasePlugin, IPluginConfig<Item_Config>
{
    public override string ModuleName => "Store";
    public override string ModuleVersion => "v23";
    public override string ModuleAuthor => "schwarper";

    public Item_Config Config { get; set; } = new();
    public List<Store_Player> GlobalStorePlayers { get; set; } = [];
    public List<Store_Item> GlobalStorePlayerItems { get; set; } = [];
    public List<Store_Equipment> GlobalStorePlayerEquipments { get; set; } = [];
    public Dictionary<CCSPlayerController, PlayerTimer> GlobalDictionaryPlayer { get; set; } = [];
    public int GlobalTickrate { get; set; } = 0;
    public static Store Instance { get; set; } = new();
    public Random Random { get; set; } = new();
    public Dictionary<CCSPlayerController, float> GlobalGiftTimeout { get; set; } = [];
    public static StoreAPI Api { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> Items { get; set; } = [];
    public Dictionary<CBaseModelEntity, CCSPlayerController> InspectList { get; set; } = [];

    public override void Load(bool hotReload)
    {
        Capabilities.RegisterPluginCapability(IStoreApi.Capability, () => Api);
        Instance = this;

        Event.Load();
        Command.Load();

        if (hotReload)
        {
            List<CCSPlayerController> players = Utilities.GetPlayers();
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
        Item_Tags.OnPluginEnd();

        List<CCSPlayerController> players = Utilities.GetPlayers();
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

    public void OnConfigParsed(Item_Config config)
    {
        Config_Config.Load();

        if (!config.Items.ValueKind.IsValueKindObject())
            throw new JsonException();

        Items = config.Items.ExtractItems();
        Config = config;
    }
}

