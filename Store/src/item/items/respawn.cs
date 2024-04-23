using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Store;

public static class Item_Respawn
{
    public static void OnPluginStart()
    {
        Item.RegisterType("respawn", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, false, false);
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        player.Respawn();

        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
}