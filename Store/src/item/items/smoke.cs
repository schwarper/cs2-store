using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_Smoke
{
    private static bool smokeExists = false;

    public static void OnPluginStart()
    {
        Item.RegisterType("smoke", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.GetItemsByType("smoke").Count > 0)
        {
            smokeExists = true;
        }
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
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
        if (!smokeExists)
        {
            return;
        }

        if (entity.DesignerName != "smokegrenade_projectile")
        {
            return;
        }

        CSmokeGrenadeProjectile grenade = new(entity.Handle);

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

            Store_Equipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "smoke");

            if (item == null)
            {
                return;
            }

            if (item.UniqueId == "colorsmoke")
            {
                grenade.SmokeColor.X = Instance.Random.NextSingle() * 255.0f;
                grenade.SmokeColor.Y = Instance.Random.NextSingle() * 255.0f;
                grenade.SmokeColor.Z = Instance.Random.NextSingle() * 255.0f;
            }
            else
            {
                Dictionary<string, string>? itemdata = Item.GetItem(item.Type, item.UniqueId);

                if (itemdata == null)
                {
                    return;
                }

                string[] colorValues = itemdata["color"].Split(' ');

                grenade.SmokeColor.X = float.Parse(colorValues[0], CultureInfo.InvariantCulture);
                grenade.SmokeColor.Y = float.Parse(colorValues[1], CultureInfo.InvariantCulture);
                grenade.SmokeColor.Z = float.Parse(colorValues[2], CultureInfo.InvariantCulture);
            }
        });
    }
}