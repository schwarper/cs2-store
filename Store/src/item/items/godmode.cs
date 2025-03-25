using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using static Store.Store;

namespace Store;

public static class Item_Godmode
{
    public static void OnPluginStart()
    {
        Item.RegisterType("godmode", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, false, true);
    }

    public static void OnMapStart() { }

    public static void OnServerPrecacheResources(ResourceManifest manifest) { }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!float.TryParse(item["godmodeTimerValue"], CultureInfo.InvariantCulture, out float godmodeTimerValue))
        {
            return false;
        }

        if (player.PlayerPawn?.Value is not { } playerPawn) return false;

        if (godmodeTimerValue > 0.0f)
        {
            Instance.AddTimer(godmodeTimerValue, () =>
            {
                playerPawn.TakesDamage = true;
                player.PrintToChatMessage("Godmode expired");
            });
        }

        playerPawn.TakesDamage = false;
        return true;
    }

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}