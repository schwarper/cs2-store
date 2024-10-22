using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Store;

public static class Item_Link
{
    public static void OnPluginStart()
    {
        Item.RegisterType("link", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, false, null);
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        player.ExecuteClientCommandFromServer(item["uniqueid"]);

        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}