using CounterStrikeSharp.API.Core;
using StoreApi;
using static StoreApi.Store;

namespace Store;

public class StoreAPI : IStoreApi
{
    public StoreAPI()
    {
    }

    public int GetPlayerCredits(CCSPlayerController player)
    {
        return Credits.Get(player);
    }

    public void SetPlayerCredits(CCSPlayerController player, int amount)
    {
        Credits.Set(player, amount);
    }

    public void GivePlayerCredits(CCSPlayerController player, int amount)
    {
        Credits.Give(player, amount);
    }

    public bool Item_Purchase(CCSPlayerController player, Dictionary<string, string> item)
    {
        return Item.Purchase(player, item);
    }

    public bool Item_Equip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return Item.Equip(player, item);
    }

    public bool Item_Unequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return Item.Unequip(player, item);
    }

    public bool Item_Sell(CCSPlayerController player, Dictionary<string, string> item)
    {
        return Item.Sell(player, item);
    }

    public bool Item_PlayerHas(CCSPlayerController player, string type, string UniqueId, bool ignoreVip)
    {
        return Item.PlayerHas(player, type, UniqueId, ignoreVip);
    }

    public bool Item_PlayerUsing(CCSPlayerController player, string type, string UniqueId)
    {
        return Item.PlayerUsing(player, type, UniqueId);
    }

    public bool Item_IsInJson(string type, string UniqueId)
    {
        return Item.IsInJson(type, UniqueId);
    }

    public Dictionary<string, string>? GetItem(string type, string UniqueId)
    {
        return Item.GetItem(type, UniqueId);
    }

    public List<KeyValuePair<string, Dictionary<string, string>>> GetItemsByType(string type)
    {
        return Item.GetItemsByType(type);
    }

    public List<Store_Item> GetPlayerItems(CCSPlayerController player)
    {
        return Item.GetPlayerItems(player);
    }

    public List<Store_Equipment> GetPlayerEquipments(CCSPlayerController player)
    {
        return Item.GetPlayerEquipments(player);
    }

    public bool IsPlayerVip(CCSPlayerController player)
    {
        return Item.IsPlayerVip(player);
    }

    public void RegisterType(string Type, Action MapStart, Func<CCSPlayerController, Dictionary<string, string>, bool> Equip, Func<CCSPlayerController, Dictionary<string, string>, bool> Unequip, bool Equipable, bool? Alive)
    {
        Item.RegisterType(Type, MapStart, Equip, Unequip, Equipable, Alive);
    }
}