using CounterStrikeSharp.API.Core;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    private void Respawn_OnPluginStart()
    {
        new StoreAPI().RegisterType("respawn", Respawn_OnMapStart, Respawn_OnEquip, Respawn_OnUnequip, false, true);
    }
    private void Respawn_OnMapStart()
    {
    }
    private bool Respawn_OnEquip(CCSPlayerController player, Store_Item item)
    {
        player.Respawn();

        return true;
    }
    private bool Respawn_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
}