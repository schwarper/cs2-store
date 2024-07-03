using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Core.Translations;
using StoreApi;
using static StoreApi.Store;

namespace Store;

public class Store : BasePlugin, IPluginConfig<StoreConfig>
{
    public override string ModuleName => "Store";
    public override string ModuleVersion => "0.2.6";
    public override string ModuleAuthor => "schwarper";

    public StoreConfig Config { get; set; } = new StoreConfig();
    public List<Store_Player> GlobalStorePlayers { get; set; } = [];
    public List<Store_Item> GlobalStorePlayerItems { get; set; } = [];
    public List<Store_Equipment> GlobalStorePlayerEquipments { get; set; } = [];
    public List<Store_Item_Types> GlobalStoreItemTypes { get; set; } = [];
    public Dictionary<CCSPlayerController, Player> GlobalDictionaryPlayer { get; set; } = [];
    public int GlobalTickrate { get; set; } = 0;
    public static Store Instance { get; set; } = new();
    public Random Random { get; set; } = new();

    public override void Load(bool hotReload)
    {
        Capabilities.RegisterPluginCapability(IStoreApi.Capability, () => new StoreAPI());

        Instance = this;

        Event.Load();
        Command.Load();
        Menu.SetSettings(hotReload);

        Item_Armor.OnPluginStart();
        Item_ColoredSkin.OnPluginStart();
        Item_CustomWeapon.OnPluginStart();
        Item_Godmode.OnPluginStart();
        Item_Gravity.OnPluginStart();
        Item_GrenadeTrail.OnPluginStart();
        Item_Health.OnPluginStart();
        Item_Open.OnPluginStart();
        Item_PlayerSkin.OnPluginStart();
        Item_Respawn.OnPluginStart();
        Item_Smoke.OnPluginStart();
        Item_Sound.OnPluginStart();
        Item_Speed.OnPluginStart();
        Item_Tracer.OnPluginStart();
        Item_Trail.OnPluginStart();
        Item_Weapon.OnPluginStart();
        Item_Equipment.OnPluginStart();

        if (hotReload)
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                Database.LoadPlayer(player);
            }
        }
    }

    public override void Unload(bool hotReload)
    {
        Event.Unload();
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        Item_Tag.OnAllPluginsLoaded();
    }

    public void OnConfigParsed(StoreConfig config)
    {
        if (string.IsNullOrEmpty(config.Database.Host) || string.IsNullOrEmpty(config.Database.Name) || string.IsNullOrEmpty(config.Database.User))
        {
            throw new Exception("You need to setup Database credentials in config.");
        }

        config.Tag = StringExtensions.ReplaceColorTags(config.Tag);

        Task.Run(async () =>
        {
            await Database.CreateDatabaseAsync(config);
        });

        Config = config;
    }
}
