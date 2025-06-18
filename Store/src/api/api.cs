using System.Reflection;
using CounterStrikeSharp.API.Core;
using StoreApi;
using static StoreApi.Store;

namespace Store;

public class StoreApi : IStoreApi
{
    public event Action<CCSPlayerController, Dictionary<string, string>>? OnPlayerPurchaseItem;
    public event Action<CCSPlayerController, Dictionary<string, string>>? OnPlayerEquipItem;
    public event Action<CCSPlayerController, Dictionary<string, string>>? OnPlayerUnequipItem;
    public event Action<CCSPlayerController, Dictionary<string, string>>? OnPlayerSellItem;

    public void PlayerPurchaseItem(CCSPlayerController player, Dictionary<string, string> item)
    {
        OnPlayerPurchaseItem?.Invoke(player, item);
    }

    public void PlayerEquipItem(CCSPlayerController player, Dictionary<string, string> item)
    {
        OnPlayerEquipItem?.Invoke(player, item);
    }

    public void PlayerUnequipItem(CCSPlayerController player, Dictionary<string, string> item)
    {
        OnPlayerUnequipItem?.Invoke(player, item);
    }

    public void PlayerSellItem(CCSPlayerController player, Dictionary<string, string> item)
    {
        OnPlayerSellItem?.Invoke(player, item);
    }

    public string GetDatabaseString()
    {
        return Database.GlobalDatabaseConnectionString;
    }

    public int GetPlayerCredits(CCSPlayerController player)
    {
        return Credits.Get(player);
    }

    public int SetPlayerCredits(CCSPlayerController player, int credits)
    {
        return Credits.Set(player, credits);
    }

    public int GetPlayerOriginalCredits(CCSPlayerController player)
    {
        return Credits.GetOriginal(player);
    }

    public int SetPlayerOriginalCredits(CCSPlayerController player, int credits)
    {
        return Credits.SetOriginal(player, credits);
    }

    public int GivePlayerCredits(CCSPlayerController player, int credits)
    {
        return Credits.Give(player, credits);
    }

    public bool Item_Give(CCSPlayerController player, Dictionary<string, string> item)
    {
        return Item.Give(player, item);
    }

    public bool Item_Purchase(CCSPlayerController player, Dictionary<string, string> item)
    {
        return Item.Purchase(player, item);
    }

    public bool Item_Equip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return Item.Equip(player, item);
    }

    public bool Item_Unequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return Item.Unequip(player, item, update);
    }

    public bool Item_Sell(CCSPlayerController player, Dictionary<string, string> item)
    {
        return Item.Sell(player, item);
    }

    public bool Item_PlayerHas(CCSPlayerController player, string type, string uniqueId, bool ignoreVip)
    {
        return Item.PlayerHas(player, type, uniqueId, ignoreVip);
    }

    public bool Item_PlayerUsing(CCSPlayerController player, string type, string uniqueId)
    {
        return Item.PlayerUsing(player, type, uniqueId);
    }

    public bool Item_IsInJson(string uniqueId)
    {
        return Item.IsInJson(uniqueId);
    }

    public bool IsPlayerVip(CCSPlayerController player)
    {
        return Item.IsPlayerVip(player);
    }

    public Dictionary<string, string>? GetItem(string uniqueId)
    {
        return Item.GetItem(uniqueId);
    }

    public List<KeyValuePair<string, Dictionary<string, string>>> GetItemsByType(string type)
    {
        return Item.GetItemsByType(type);
    }

    public List<StoreItem> GetPlayerItems(CCSPlayerController player, string? type)
    {
        return Item.GetPlayerItems(player, type);
    }

    public List<StoreEquipment> GetPlayerEquipments(CCSPlayerController player, string? type)
    {
        return Item.GetPlayerEquipments(player, type);
    }

    public void RegisterModules(Assembly assembly)
    {
        ItemModuleManager.RegisterModules(assembly);
    }
}