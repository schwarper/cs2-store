using CounterStrikeSharp.API.Core;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public static void Godmode_OnPluginStart()
    {
        new StoreAPI().RegisterType("godmode", Godmode_OnMapStart, Godmode_OnEquip, Godmode_OnUnequip, false, true);
    }
    public static void Godmode_OnMapStart()
    {
    }
    public static bool Godmode_OnEquip(CCSPlayerController player, Store_Item item)
    {
        if (!float.TryParse(item.UniqueId, out float godmode))
        {
            return false;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
        {
            return false;
        }

        if (godmode > 0.0)
        {
            Instance.AddTimer(godmode, () =>
            {
                playerPawn.TakesDamage = true;

                player.PrintToChatMessage("Godmode expired");
            });
        }

        playerPawn.TakesDamage = false;

        return true;
    }
    public static bool Godmode_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
}