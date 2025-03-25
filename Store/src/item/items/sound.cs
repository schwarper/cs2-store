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

    public static void OnMapStart() { }

    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        Item.GetItemsByType("sound").ForEach(item => manifest.AddResource(item.Value["sound"]));
    }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        Utilities.GetPlayers()
            .Where(target => target != null && target.IsValid)
            .ToList()
            .ForEach(target => target.ExecuteClientCommand($"play {item["sound"]}"));

        return true;
    }

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}