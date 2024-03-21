using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    private void Trail_OnPluginStart()
    {
        new StoreAPI().RegisterType("trail", Trail_OnMapStart, Trail_OnEquip, Trail_OnUnequip, true, null);
    }
    private void Trail_OnMapStart()
    {
        IEnumerable<string> playerTrails = Config.Items
        .SelectMany(wk => wk.Value)
        .Where(kvp => kvp.Value.Type == "trail")
        .Select(kvp => kvp.Value.UniqueId);

        RegisterListener<OnServerPrecacheResources>((manifest) =>
        {
            foreach (string UniqueId in playerTrails)
            {
                manifest.AddResource(UniqueId);
            }
        });
    }
    private bool Trail_OnEquip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
    private bool Trail_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
    public void OnTick_CreateTrail(CCSPlayerController player)
    {
        Store_PlayerItem? playertrail = GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "trail");

        if (playertrail == null)
        {
            return;
        }

        CParticleSystem? entity = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");

        if (entity == null || !entity.IsValid)
        {
            return;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
        {
            return;
        }

        if (playerPawn.AbsOrigin == null)
        {
            return;
        }

        entity.EffectName = playertrail.UniqueId;
        entity.DispatchSpawn();
        entity.Teleport(playerPawn.AbsOrigin, new QAngle(90, 0, 0), new Vector());
        entity.AcceptInput("Start");

        AddTimer(1.3f, () =>
        {
            if (entity != null && entity.IsValid)
            {
                entity.Remove();
            }
        });
    }
}