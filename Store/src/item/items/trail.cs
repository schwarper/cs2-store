using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    static readonly Vector[] GlobalTrailLastOrigin = new Vector[64];

    public static void Trail_OnPluginStart()
    {
        new StoreAPI().RegisterType("trail", Trail_OnMapStart, Trail_OnEquip, Trail_OnUnequip, true, null);

        for (int i = 0; i < 64; i++)
        {
            GlobalTrailLastOrigin[i] = new();
        }
    }
    public static void Trail_OnMapStart()
    {
        IEnumerable<string> playerTrails = Instance.Config.Items
        .SelectMany(wk => wk.Value)
        .Where(kvp => kvp.Value["type"] == "trail")
        .Select(kvp => kvp.Value["uniqueid"]);

        Instance.RegisterListener<OnServerPrecacheResources>((manifest) =>
        {
            foreach (string UniqueId in playerTrails)
            {
                manifest.AddResource(UniqueId);
            }
        });
    }
    public static bool Trail_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
    public static bool Trail_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
    public static void OnTick_CreateTrail(CCSPlayerController player)
    {
        Store_Equipment? playertrail = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "trail");

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

        if (Vec.CalculateDistance(GlobalTrailLastOrigin[player.Slot], playerPawn.AbsOrigin) <= 5.0f)
        {
            return;
        }

        entity.EffectName = playertrail.UniqueId;
        entity.DispatchSpawn();
        entity.Teleport(playerPawn.AbsOrigin, new QAngle(90, 0, 0), new Vector());
        entity.AcceptInput("Start");

        GlobalTrailLastOrigin[player.Slot].X = playerPawn.AbsOrigin.X;
        GlobalTrailLastOrigin[player.Slot].Y = playerPawn.AbsOrigin.Y;
        GlobalTrailLastOrigin[player.Slot].Z = playerPawn.AbsOrigin.Z;

        Dictionary<string, string>? itemdata = Item.Find(playertrail.Type, playertrail.UniqueId);

        if (itemdata == null)
        {
            return;
        }

        float lifetime = 1.3f;

        if (itemdata.TryGetValue("lifetime", out string? value) && float.TryParse(value, out float lt))
        {
            lifetime = lt;
        }

        Instance.AddTimer(lifetime, () =>
        {
            if (entity != null && entity.IsValid)
            {
                entity.Remove();
            }
        });
    }
}