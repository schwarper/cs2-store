using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using StoreApi;
using static StoreApi.Store;

namespace Store;

public class StoreAPI : IStoreApi
{
    public event Action<CCSPlayerController, Dictionary<string, string>>? OnPlayerPurchaseItem;
    public event Action<CCSPlayerController, Dictionary<string, string>>? OnPlayerEquipItem;
    public event Action<CCSPlayerController, Dictionary<string, string>>? OnPlayerUnequipItem;
    public event Action<CCSPlayerController, Dictionary<string, string>>? OnPlayerSellItem;

    public void PlayerPurchaseItem(CCSPlayerController player, Dictionary<string, string> item) =>
        OnPlayerPurchaseItem?.Invoke(player, item);

    public void PlayerEquipItem(CCSPlayerController player, Dictionary<string, string> item) =>
        OnPlayerEquipItem?.Invoke(player, item);

    public void PlayerUnequipItem(CCSPlayerController player, Dictionary<string, string> item) =>
        OnPlayerUnequipItem?.Invoke(player, item);

    public void PlayerSellItem(CCSPlayerController player, Dictionary<string, string> item) =>
        OnPlayerSellItem?.Invoke(player, item);

    public string GetDatabaseString() =>
        Database.GlobalDatabaseConnectionString;

    public int GetPlayerCredits(CCSPlayerController player) =>
        Credits.Get(player);

    public int SetPlayerCredits(CCSPlayerController player, int credits) =>
        Credits.Set(player, credits);

    public int GetPlayerOriginalCredits(CCSPlayerController player) =>
        Credits.GetOriginal(player);

    public int SetPlayerOriginalCredits(CCSPlayerController player, int credits) =>
        Credits.SetOriginal(player, credits);

    public int GivePlayerCredits(CCSPlayerController player, int credits) =>
        Credits.Give(player, credits);

    public bool Item_Give(CCSPlayerController player, Dictionary<string, string> item) =>
        Item.Give(player, item);

    public bool Item_Purchase(CCSPlayerController player, Dictionary<string, string> item) =>
        Item.Purchase(player, item);

    public bool Item_Equip(CCSPlayerController player, Dictionary<string, string> item) =>
        Item.Equip(player, item);

    public bool Item_Unequip(CCSPlayerController player, Dictionary<string, string> item, bool update) =>
        Item.Unequip(player, item, update);

    public bool Item_Sell(CCSPlayerController player, Dictionary<string, string> item) =>
        Item.Sell(player, item);

    public bool Item_PlayerHas(CCSPlayerController player, string type, string uniqueId, bool ignoreVip) =>
        Item.PlayerHas(player, type, uniqueId, ignoreVip);

    public bool Item_PlayerUsing(CCSPlayerController player, string type, string uniqueId) =>
        Item.PlayerUsing(player, type, uniqueId);

    public bool Item_IsInJson(string uniqueId) =>
        Item.IsInJson(uniqueId);

    public bool IsPlayerVip(CCSPlayerController player) =>
        Item.IsPlayerVip(player);

    public Dictionary<string, string>? GetItem(string uniqueId) =>
        Item.GetItem(uniqueId);

    public List<KeyValuePair<string, Dictionary<string, string>>> GetItemsByType(string type) =>
        Item.GetItemsByType(type);

    public List<Store_Item> GetPlayerItems(CCSPlayerController player) =>
        Item.GetPlayerItems(player);

    public List<Store_Equipment> GetPlayerEquipments(CCSPlayerController player) =>
        Item.GetPlayerEquipments(player);

    public void RegisterType(string Type, Action MapStart, Action<ResourceManifest> ServerPrecacheResources, Func<CCSPlayerController, Dictionary<string, string>, bool> Equip, Func<CCSPlayerController, Dictionary<string, string>, bool, bool> Unequip, bool Equipable, bool? Alive) =>
        Item.RegisterType(Type, MapStart, ServerPrecacheResources, Equip, Unequip, Equipable, Alive);
}