using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public static void Smoke_OnPluginStart()
    {
        new StoreAPI().RegisterType("smoke", Smoke_OnMapStart, Smoke_OnEquip, Smoke_OnUnequip, true, null);
    }
    public static void Smoke_OnMapStart()
    {
    }
    public static bool Smoke_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
    public static bool Smoke_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
    public static void OnEntityCreated_Smoke(CEntityInstance entity)
    {
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
                grenade.SmokeColor.X = Random.Shared.NextSingle() * 255.0f;
                grenade.SmokeColor.Y = Random.Shared.NextSingle() * 255.0f;
                grenade.SmokeColor.Z = Random.Shared.NextSingle() * 255.0f;
            }
            else
            {
                Dictionary<string, string>? itemdata = Item.Find(item.Type, item.UniqueId);

                if (itemdata == null)
                {
                    return;
                }

                string[] colorValues = itemdata["color"].Split(' ');

                grenade.SmokeColor.X = float.Parse(colorValues[0]);
                grenade.SmokeColor.Y = float.Parse(colorValues[1]);
                grenade.SmokeColor.Z = float.Parse(colorValues[2]);
            }
        });
    }
}