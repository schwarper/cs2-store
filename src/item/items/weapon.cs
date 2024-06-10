using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Store;

public static class Item_Weapon
{
    public static void OnPluginStart()
    {
        Item.RegisterType("weapon", OnMapStart, ServerPrecacheResources, OnEquip, OnUnequip, false, true);
    }
    public static void OnMapStart()
    {
    }
    public static void ServerPrecacheResources(ResourceManifest manifest)
    {
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (GameRules.IsPistolRound() && item["no_pistol_round"] == "true")
        {
            player.PrintToChatMessage("No in pistol round", item["name"]);
            return false;
        }

        player.GiveNamedItem(item["uniqueid"]);

        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
}
