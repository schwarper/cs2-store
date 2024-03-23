using CounterStrikeSharp.API.Core;
using static StoreApi.Store;

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
    public static bool Respawn_OnEquip(CCSPlayerController player, Store_Item item)
    {
        player.Respawn();

        return true;
    }
    public static bool Respawn_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
}