using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public Dictionary<CBaseCSGrenadeProjectile, CParticleSystem> GlobalGrenadeTrail { get; set; } = new();

    public static void GrenadeTrail_OnPluginStart()
    {
        Item.RegisterType("grenadetrail", GrenadeTrail_OnMapStart, GrenadeTrail_OnEquip, GrenadeTrail_OnUnequip, true, null);
    }
    public static void GrenadeTrail_OnMapStart()
    {
        Instance.RegisterListener<OnServerPrecacheResources>((manifest) =>
        {
            List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("grenadetrail");

            foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
            {
                manifest.AddResource(item.Value["uniqueid"]);
            }
        });
    }
    public static bool GrenadeTrail_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
    public static bool GrenadeTrail_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }

    public static void OnEntityCreated_GrenadeTrail(CEntityInstance entity)
    {
        if (entity.DesignerName != "hegrenade_projectile")
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

            Store_Equipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "grenadetrail");

            if (item == null)
            {
                return;
            }

            CParticleSystem? particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");

            if (particle == null || !particle.IsValid)
            {
                return;
            }

            Dictionary<string, string>? itemdata = Item.GetItem(item.Type, item.UniqueId);

            if (itemdata == null)
            {
                return;
            }

            if (!itemdata.TryGetValue("acceptInputValue", out string? acceptinputvalue) || string.IsNullOrEmpty(acceptinputvalue))
            {
                acceptinputvalue = "Start";
            }

            particle.EffectName = item.UniqueId;
            particle.DispatchSpawn();
            particle.Teleport(grenade.AbsOrigin!, new QAngle(), new Vector());
            particle.AcceptInput(acceptinputvalue);

            Instance.GlobalGrenadeTrail.Add(grenade, particle);
        });
    }

    public static void OnTick_GrenadeTrail()
    {
        foreach (KeyValuePair<CBaseCSGrenadeProjectile, CParticleSystem> kv in Instance.GlobalGrenadeTrail)
        {
            CBaseCSGrenadeProjectile grenade = kv.Key;
            CParticleSystem particle = kv.Value;

            if (!grenade.IsValid || Vec.CalculateDistance(grenade.AbsOrigin!, grenade.ExplodeEffectOrigin) < 5)
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