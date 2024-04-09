using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using StoreApi;

namespace Store;

public partial class Store : BasePlugin, IPluginConfig<StoreConfig>
{
    public override string ModuleName => "Store";
    public override string ModuleVersion => "0.1.2a";
    public override string ModuleAuthor => "schwarper & xshadowbringer";

    public override void Load(bool hotReload)
    {
        Capabilities.RegisterPluginCapability(IStoreApi.Capability, () => new StoreAPI());

        Instance = this;

        Event.Load();
        Command.Load();

        Armor_OnPluginStart();
        ColoredSkin_OnPluginStart();
        Godmode_OnPluginStart();
        Gravity_OnPluginStart();
        GrenadeTrail_OnPluginStart();
        Health_OnPluginStart();
        Open_OnPluginStart();
        Playerskin_OnPluginStart();
        Respawn_OnPluginStart();
        Smoke_OnPluginStart();
        Sound_OnPluginStart();
        Speed_OnPluginStart();
        Tracer_OnPluginStart();
        Trail_OnPluginStart();
        Weapon_OnPluginStart();

        if (hotReload)
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || string.IsNullOrEmpty(player.IpAddress))
                {
                    continue;
                }

                Task.Run(async () =>
                {
                    await Database.LoadPlayer(player);
                });
            }
        }
    }

    public void OnConfigParsed(StoreConfig config)
    {
        Database.CreateDatabase(config);

        Config = config;
    }
}
