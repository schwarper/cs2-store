using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Store;

namespace Store;

public static class Item_Armor
{
    public static void OnPluginStart()
    {
        Item.RegisterType("armor", OnMapStart, OnServerPrecacheResources, OnEquip, OnUneuip, false, true);
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!int.TryParse(item["armorValue"], out int armor))
        {
            return false;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
        {
            return false;
        }

        var maxarmor = Instance.Config.Settings.max_armor;

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

        if (maxarmor == -1)
        {
            maxarmor = 100;

            if (playerPawn.ArmorValue >= maxarmor)
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

        Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_ArmorValue");

        return true;
    }
    public static bool OnUneuip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
}
