using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Store;

public static class Item_Sound
{
    public static void OnPluginStart()
    {
        Item.RegisterType("sound", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, false, null);
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("sound");

        foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
        {
            manifest.AddResource(item.Value["uniqueid"]);
        }
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        foreach (CCSPlayerController target in Utilities.GetPlayers())
        {
            if (target == null || !target.IsValid)
            {
                continue;
            }

            target.ExecuteClientCommand($"play {item["uniqueid"]}");
        }

        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}