using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static StoreApi.Store;

namespace Store;

[StoreItemType("sound")]
public class ItemSound : IItemModule
{
    public bool Equipable => false;
    public bool? RequiresAlive => null;

    public void OnPluginStart() { }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        Item.GetItemsByType("sound").ForEach(item => manifest.AddResource(item.Value["sound"]));
    }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        Utilities.GetPlayers()
            .Where(target => target.IsValid)
            .ToList()
            .ForEach(target => target.ExecuteClientCommand($"play {item["sound"]}"));

        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}