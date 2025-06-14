using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

[StoreItemType("smoke")]
public class Item_Smoke : IItemModule
{
    private static bool smokeExists = false;
    
    public bool Equipable => true;
    public bool? RequiresAlive => null;

    public void OnPluginStart()
    {
        smokeExists = Item.IsAnyItemExistInType("smoke");
    }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest) { }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }

    public static void OnEntityCreated(CEntityInstance entity)
    {
        if (!smokeExists || entity.DesignerName != "smokegrenade_projectile")
            return;

        CSmokeGrenadeProjectile grenade = new(entity.Handle);
        if (grenade.Handle == IntPtr.Zero)
            return;

        Server.NextFrame(() =>
        {
            CBasePlayerController? player = grenade.Thrower.Value?.Controller.Value;
            if (player == null)
                return;

            Store_Equipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "smoke");
            if (item == null)
                return;

            if (item.UniqueId == "colorsmoke")
            {
                grenade.SmokeColor.X = Instance.Random.NextSingle() * 255.0f;
                grenade.SmokeColor.Y = Instance.Random.NextSingle() * 255.0f;
                grenade.SmokeColor.Z = Instance.Random.NextSingle() * 255.0f;
            }
            else
            {
                Dictionary<string, string>? itemdata = Item.GetItem(item.UniqueId);
                if (itemdata == null)
                    return;

                string[] colorValues = itemdata["color"].Split(' ');
                grenade.SmokeColor.X = float.Parse(colorValues[0], CultureInfo.InvariantCulture);
                grenade.SmokeColor.Y = float.Parse(colorValues[1], CultureInfo.InvariantCulture);
                grenade.SmokeColor.Z = float.Parse(colorValues[2], CultureInfo.InvariantCulture);
            }
        });
    }
}