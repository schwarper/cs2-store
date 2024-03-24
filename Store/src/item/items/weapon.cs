using CounterStrikeSharp.API.Core;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public static void Weapon_OnPluginStart()
    {
        new StoreAPI().RegisterType("weapon", Weapon_OnMapStart, Weapon_OnEquip, Weapon_OnUnequip, false, true);
    }
    public static void Weapon_OnMapStart()
    {
    }
    public static bool Weapon_OnEquip(CCSPlayerController player, Store_Item item)
    {
        switch (PlayerUtils.IsPistolRound())
        {
            case true:
                if (item.Slot != 1)
                {
                    player.GiveNamedItem(item.UniqueId);
                }
                break;
            default:
                player.GiveNamedItem(item.UniqueId);
                break;
        }

        return true;
    }
    public static bool Weapon_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
}