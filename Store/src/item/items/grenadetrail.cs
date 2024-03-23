using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public Dictionary<CBaseCSGrenadeProjectile, CParticleSystem> GlobalGrenadeTrail { get; set; } = new();

    public static void GrenadeTrail_OnPluginStart()
    {
        new StoreAPI().RegisterType("grenadetrail", GrenadeTrail_OnMapStart, GrenadeTrail_OnEquip, GrenadeTrail_OnUnequip, true, null);
    }
    public static void GrenadeTrail_OnMapStart()
    {
    }
    public static bool GrenadeTrail_OnEquip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
    public static bool GrenadeTrail_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }

    public static void OnEntityCreated_GrenadeTrail(CEntityInstance entity)
    {
        if (!entity.DesignerName.Contains("_projectile"))
        {
            return;
        }

        CBaseCSGrenadeProjectile grenade = new(entity.Handle);

        if (grenade.Handle == IntPtr.Zero)
        {
            return;
        }

        Server.NextFrame(() =>
        {
            CBasePlayerController? player = grenade.Thrower.Value?.Controller.Value;

            if (player == null)
            {
                return;
            }

            Store_PlayerItem? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "grenadetrail");

            if (item == null)
            {
                return;
            }

            CParticleSystem? particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");

            if (particle == null || !particle.IsValid)
            {
                return;
            }

            particle.EffectName = item.UniqueId;
            particle.DispatchSpawn();
            particle.Teleport(grenade.AbsOrigin!, new QAngle(), new Vector());
            particle.AcceptInput("Start");

            Instance.GlobalGrenadeTrail.Add(grenade, particle);
        });
    }

    public static float CalculateDistance(Vector point1, Vector point2)
    {
        float dx = point2.X - point1.X;
        float dy = point2.Y - point1.Y;
        float dz = point2.Z - point1.Z;

        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static void OnTick_GrenadeTrail()
    {
        foreach (KeyValuePair<CBaseCSGrenadeProjectile, CParticleSystem> hey in Instance.GlobalGrenadeTrail)
        {
            CBaseCSGrenadeProjectile grenade = hey.Key;
            CParticleSystem particle = hey.Value;

            if (!grenade.IsValid || CalculateDistance(grenade.AbsOrigin!, grenade.ExplodeEffectOrigin) < 5)
            {
                if (particle.IsValid)
                {
                    particle.Remove();
                    Instance.GlobalGrenadeTrail.Remove(grenade);
                }

                return;
            }

            particle.Teleport(grenade.AbsOrigin!, grenade.AbsRotation!, grenade.AbsVelocity);
        }
    }
}