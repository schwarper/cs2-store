using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static Dapper.SqlMapper;
using static StoreApi.Store;

namespace Store;

public partial class Store : BasePlugin
{
    private void Armor_OnPluginStart()
    {
        new StoreAPI().RegisterType("armor", Armor_OnMapStart, Armor_OnEquip, Armor_OnUnequip, false, true);
    }
    private void Armor_OnMapStart()
    {
    }
    private bool Armor_OnEquip(CCSPlayerController player, Store_Item item)
    {
        if (!int.TryParse(item.UniqueId, out int armor))
        {
            return false;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
        {
            return false;
        }

        if (!int.TryParse(Instance.Config.Settings["max_armor"], out int maxarmor))
        {
            return false;
        }

        if (maxarmor > 0)
        {
            if (playerPawn.ArmorValue >= maxarmor)
            {
                return false;
            }

            if (playerPawn.ArmorValue + armor > maxarmor)
            {
                armor = maxarmor - playerPawn.ArmorValue;
            }
        }

        if (maxarmor > 0)
        {
            if (playerPawn.ArmorValue == maxarmor)
            {
                return false;
            }

            if (playerPawn.ArmorValue + armor > maxarmor)
            {
                armor = maxarmor - playerPawn.ArmorValue;
            }
        }

        if (playerPawn.ItemServices != null)
        {
            new CCSPlayer_ItemServices(playerPawn.ItemServices.Handle).HasHelmet = true;
        }

        playerPawn.ArmorValue += armor;

        Utilities.SetStateChanged(playerPawn, "CCSPlayerPawnBase", "m_ArmorValue");

        return true;
    }
    private bool Armor_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
}