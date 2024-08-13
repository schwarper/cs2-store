using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;

namespace Store;

public static class Item_Gravity
{
    public static void OnPluginStart()
    {
        Item.RegisterType("gravity", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, false, true);
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!float.TryParse(item["gravityValue"], CultureInfo.InvariantCulture, out float gravity))
        {
            return false;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
        {
            return false;
        }

        playerPawn.GravityScale = gravity;

        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}