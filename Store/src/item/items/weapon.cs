using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using static StoreApi.Store;

namespace Store;

[StoreItemType("weapon")]
public class Item_Weapon : IItemModule
{
    public bool Equipable => false;
    public bool? RequiresAlive => true;

    public void OnPluginStart() { }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest) { }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (item.TryGetValue("no_pistol_round", out string? nopistolround) && nopistolround == "true" && GameRules.IsPistolRound())
        {
            player.PrintToChatMessage("No in pistol round", Item.GetItemName(player, item));
            return false;
        }

        player.GiveNamedItem(item["weapon"]);
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}