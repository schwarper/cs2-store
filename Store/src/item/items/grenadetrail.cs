using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_GrenadeTrail
{
    public static Dictionary<CBaseCSGrenadeProjectile, CParticleSystem> GlobalGrenadeTrail { get; set; } = [];
    private static bool grenadetrailExists = false;

    public static void OnPluginStart()
    {
        Item.RegisterType("grenadetrail", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.IsAnyItemExistInType("grenadetrail"))
        {
            grenadetrailExists = true;
        }
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("grenadetrail");

        foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
        {
            manifest.AddResource(item.Value["model"]);
        }
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }

    public static void OnEntityCreated(CEntityInstance entity)
    {
        if (!grenadetrailExists)
        {
            return;
        }

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

            Dictionary<string, string>? itemdata = Item.GetItem(item.UniqueId);

            if (itemdata == null)
            {
                return;
            }

            if (!itemdata.TryGetValue("acceptInputValue", out string? acceptinputvalue) || string.IsNullOrEmpty(acceptinputvalue))
            {
                acceptinputvalue = "Start";
            }

            particle.EffectName = itemdata["model"];
            particle.DispatchSpawn();
            particle.Teleport(grenade.AbsOrigin!, new QAngle(), new Vector());
            particle.AcceptInput(acceptinputvalue);

            GlobalGrenadeTrail.Add(grenade, particle);
        });
    }

    public static void OnTick()
    {
        if (!grenadetrailExists)
        {
            return;
        }

        foreach (KeyValuePair<CBaseCSGrenadeProjectile, CParticleSystem> kv in GlobalGrenadeTrail)
        {
            CBaseCSGrenadeProjectile grenade = kv.Key;
            CParticleSystem particle = kv.Value;

            if (!grenade.IsValid || Vec.CalculateDistance(grenade.AbsOrigin!, grenade.ExplodeEffectOrigin) < 5)
            {
                if (particle.IsValid)
                {
                    particle.Remove();
                    GlobalGrenadeTrail.Remove(grenade);
                }

                return;
            }

            particle.Teleport(grenade.AbsOrigin!, grenade.AbsRotation!, grenade.AbsVelocity);
        }
    }
}