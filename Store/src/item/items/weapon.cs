using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Store;

public static class Item_Weapon
{
    public static void OnPluginStart() =>
        Item.RegisterType("weapon", OnMapStart, ServerPrecacheResources, OnEquip, OnUnequip, false, true);

    public static void OnMapStart() { }

    public static void ServerPrecacheResources(ResourceManifest manifest) { }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (item.TryGetValue("no_pistol_round", out string? nopistolround) && nopistolround == "true" && GameRules.IsPistolRound())
        {
            player.PrintToChatMessage("No in pistol round", Item.GetItemName(player, item));
            return false;
        }

        player.GiveNamedItem(item["weapon"]);
        return true;
    }

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update) => true;
}