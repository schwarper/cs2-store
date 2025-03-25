using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using static Store.Store;

namespace Store;

public static class Item_Speed
{
    public static void OnPluginStart()
    {
        Item.RegisterType("speed", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, false, true);
    }

    public static void OnMapStart() { }

    public static void OnServerPrecacheResources(ResourceManifest manifest) { }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!float.TryParse(item["speedValue"], CultureInfo.InvariantCulture, out float speed) ||
            !float.TryParse(item["speedTimerValue"], CultureInfo.InvariantCulture, out float speedtimer))
            return false;

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null)
            return false;

        playerPawn.VelocityModifier = speed;

        if (speedtimer > 0.0)
        {
            Instance.AddTimer(speedtimer, () =>
            {
                playerPawn.VelocityModifier = 1.0f;
                player.PrintToChatMessage("Speed expired");
            });
        }

        return true;
    }

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}