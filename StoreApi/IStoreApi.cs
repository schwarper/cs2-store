using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using static StoreApi.Store;

namespace StoreApi;

public interface IStoreApi
{
    static readonly PluginCapability<IStoreApi?> Capability = new("cs2-store:api");

    event Action<CCSPlayerController, Dictionary<string, string>>? OnPlayerPurchaseItem;
    event Action<CCSPlayerController, Dictionary<string, string>>? OnPlayerEquipItem;
    event Action<CCSPlayerController, Dictionary<string, string>>? OnPlayerUnequipItem;
    event Action<CCSPlayerController, Dictionary<string, string>>? OnPlayerSellItem;

    string GetDatabaseString();
    int GetPlayerCredits(CCSPlayerController player);
    int SetPlayerCredits(CCSPlayerController player, int credits);
    int GetPlayerOriginalCredits(CCSPlayerController player);
    int SetPlayerOriginalCredits(CCSPlayerController player, int credits);
    int GivePlayerCredits(CCSPlayerController player, int credits);
    bool Item_Give(CCSPlayerController player, Dictionary<string, string> item);
    bool Item_Purchase(CCSPlayerController player, Dictionary<string, string> item);
    bool Item_Equip(CCSPlayerController player, Dictionary<string, string> item);
    bool Item_Unequip(CCSPlayerController player, Dictionary<string, string> item, bool update);
    bool Item_Sell(CCSPlayerController player, Dictionary<string, string> item);
    bool Item_PlayerHas(CCSPlayerController player, string type, string uniqueId, bool ignoreVip);
    bool Item_PlayerUsing(CCSPlayerController player, string type, string uniqueId);
    bool Item_IsInJson(string uniqueId);
    bool IsPlayerVip(CCSPlayerController player);
    Dictionary<string, string>? GetItem(string uniqueId);
    List<KeyValuePair<string, Dictionary<string, string>>> GetItemsByType(string type);
    List<Store_Item> GetPlayerItems(CCSPlayerController player, string? type);
    List<Store_Equipment> GetPlayerEquipments(CCSPlayerController player, string? type);
    void RegisterType(string type, Action mapStart, Action<ResourceManifest> ServerPrecacheResources, Func<CCSPlayerController, Dictionary<string, string>, bool> equip, Func<CCSPlayerController, Dictionary<string, string>, bool, bool> unequip, bool equipable, bool? alive = false);
}