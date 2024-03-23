using CounterStrikeSharp.API.Core;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public static void Speed_OnPluginStart()
    {
        new StoreAPI().RegisterType("speed", Speed_OnMapStart, Speed_OnEquip, Speed_OnUnequip, false, true);
    }
    public static void Speed_OnMapStart()
    {
    }
    public static bool Speed_OnEquip(CCSPlayerController player, Store_Item item)
    {
        if (!float.TryParse(item.UniqueId, out float speed))
        {
            return false;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
        {
            return false;
        }

        if (speed > 0.0)
        {
            Instance.AddTimer(speed, () =>
            {
                playerPawn.VelocityModifier = 1.0f;

                player.PrintToChatMessage("Speed expired");
            });
        }

        playerPawn.VelocityModifier = speed;

        return true;
    }
    public static bool Speed_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
}