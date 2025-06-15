using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static StoreApi.Store;

namespace Store;

[StoreItemType("link")]
public class Item_Link : IItemModule
{
    public bool Equipable => false;
    public bool? RequiresAlive => null;

    public void OnPluginStart() { }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest) { }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        player.ExecuteClientCommandFromServer(item["link"]);
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}