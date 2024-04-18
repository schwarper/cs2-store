using CounterStrikeSharp.API.Core;

namespace Store;

public partial class Store
{
    public static void Weapon_OnPluginStart()
    {
        Item.RegisterType("weapon", Weapon_OnMapStart, Weapon_OnEquip, Weapon_OnUnequip, false, true);
    }
    public static void Weapon_OnMapStart()
    {
    }
    public static bool Weapon_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (GameRules.IsPistolRound() && item["no_pistol_round"] == "true")
        {
            player.PrintToChatMessage("No in pistol round", item["name"]);
            return false;
        }

        player.GiveNamedItem(item["uniqueid"]);

        return true;
    }
    public static bool Weapon_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
}
