using CounterStrikeSharp.API.Core;

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
    public static bool Godmode_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!float.TryParse(item["uniqueid"], out float godmode))
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
    public static bool Godmode_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
}