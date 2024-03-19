using CounterStrikeSharp.API.Core;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    private void Weapon_OnPluginStart()
    {
        new StoreAPI().RegisterType("weapon", Weapon_OnMapStart, Weapon_OnEquip, Weapon_OnUnequip, false, true);
    }
    private void Weapon_OnMapStart()
    {
    }
    private bool Weapon_OnEquip(CCSPlayerController player, Store_Item item)
    {
        player.GiveNamedItem(item.UniqueId);

        return true;
    }
    private bool Weapon_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
}