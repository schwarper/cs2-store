using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using static Store.Config_Config;

namespace Store;

public static class Item_Armor
{
    public static void OnPluginStart()
    {
        Item.RegisterType("armor", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, false, true);
    }

    public static void OnMapStart() { }

    public static void OnServerPrecacheResources(ResourceManifest manifest) { }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!int.TryParse(item["armorValue"], out int armor)) return false;
        if (player.PlayerPawn?.Value is not { } playerPawn) return false;

        int maxArmor = Config.Settings.MaxArmor;
        if (maxArmor > 0 && playerPawn.ArmorValue >= maxArmor) return false;

        playerPawn.GiveArmor(Math.Min(armor, maxArmor - playerPawn.ArmorValue));

        return true;
    }

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}