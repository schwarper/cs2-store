using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using static StoreApi.Store;

namespace StoreApi;

public interface IStoreApi
{
    public static readonly PluginCapability<IStoreApi?> Capability = new("store:api");

    public int GetPlayerCredits(CCSPlayerController player);
    public int SetPlayerCredits(CCSPlayerController player, int credits);
    public int GetPlayerOriginalCredits(CCSPlayerController player);
    public int SetPlayerOriginalCredits(CCSPlayerController player, int credits);
    public int GivePlayerCredits(CCSPlayerController player, int credits);
    public bool Item_Purchase(CCSPlayerController player, Dictionary<string, string> item);
    public bool Item_Equip(CCSPlayerController player, Dictionary<string, string> item);
    public bool Item_Unequip(CCSPlayerController player, Dictionary<string, string> item);
    public bool Item_Sell(CCSPlayerController player, Dictionary<string, string> item);
    public bool Item_PlayerHas(CCSPlayerController player, string type, string uniqueId, bool ignoreVip);
    public bool Item_PlayerUsing(CCSPlayerController player, string type, string uniqueId);
    public bool Item_IsInJson(string type, string uniqueId);
    public Dictionary<string, string>? GetItem(string type, string uniqueId);
    public List<KeyValuePair<string, Dictionary<string, string>>> GetItemsByType(string type);
    public List<Store_Item> GetPlayerItems(CCSPlayerController player);
    public List<Store_Equipment> GetPlayerEquipments(CCSPlayerController player);
    public bool IsPlayerVip(CCSPlayerController player);
    public void RegisterType(string type, Action mapStart, Action<ResourceManifest> ServerPrecacheResources, Func<CCSPlayerController, Dictionary<string, string>, bool> equip, Func<CCSPlayerController, Dictionary<string, string>, bool> unequip, bool equipable, bool? alive = false);

}