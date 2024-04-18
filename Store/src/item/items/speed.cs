using CounterStrikeSharp.API.Core;
using System.Globalization;

namespace Store;

public partial class Store
{
    public static void Speed_OnPluginStart()
    {
        Item.RegisterType("speed", Speed_OnMapStart, Speed_OnEquip, Speed_OnUnequip, false, true);
    }
    public static void Speed_OnMapStart()
    {
    }
    public static bool Speed_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!float.TryParse(item["speedValue"], CultureInfo.InvariantCulture, out float speed))
        {
            return false;
        }

        if (!float.TryParse(item["speedTimerValue"], CultureInfo.InvariantCulture, out float speedtimer))
        {
            return false;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
        {
            return false;
        }

        if (speedtimer > 0.0)
        {
            Instance.AddTimer(speedtimer, () =>
            {
                playerPawn.VelocityModifier = 1.0f;

                player.PrintToChatMessage("Speed expired");
            });
        }

        playerPawn.VelocityModifier = speed;

        return true;
    }
    public static bool Speed_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
}