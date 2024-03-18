using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;

namespace Store;

public partial class Store : BasePlugin, IPluginConfig<StoreConfig>
{
    public override string ModuleName => "Store";
    public override string ModuleVersion => "0.0.2";
    public override string ModuleAuthor => "schwarper";

    public override void Load(bool hotReload)
    {
        Instance = this;

        Event.Load();
        Command.Load();

        Capabilities.RegisterPluginCapability(StoreAPI, () => new StoreAPI());

        Armor_OnPluginStart();
        Godmode_OnPluginStart();
        Gravity_OnPluginStart();
        Health_OnPluginStart();
        Playerskin_OnPluginStart();
        Respawn_OnPluginStart();
        Smoke_OnPluginStart();
        Speed_OnPluginStart();
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