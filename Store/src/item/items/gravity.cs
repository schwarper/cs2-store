using CounterStrikeSharp.API.Core;

namespace Store;

public partial class Store
{
    public static void Gravity_OnPluginStart()
    {
        new StoreAPI().RegisterType("gravity", Gravity_OnMapStart, Gravity_OnEquip, Gravity_OnUnequip, false, true);
    }
    public static void Gravity_OnMapStart()
    {
    }
    public static bool Gravity_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!float.TryParse(item["uniqueid"], out float gravity))
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
    public static bool Gravity_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
}