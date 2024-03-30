using CounterStrikeSharp.API.Core;

namespace Store;

public partial class Store
{
    public static void Respawn_OnPluginStart()
    {
        new StoreAPI().RegisterType("respawn", Respawn_OnMapStart, Respawn_OnEquip, Respawn_OnUnequip, false, false);
    }
    public static void Respawn_OnMapStart()
    {
    }
    public static bool Respawn_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        player.Respawn();

        return true;
    }
    public static bool Respawn_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
}