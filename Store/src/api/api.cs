using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using StoreApi;
using static StoreApi.Store;

namespace Store;

public class StoreAPI : IStoreApi
{
    public StoreAPI()
    {
    }

    public string GetDatabaseString()
    {
        return Database.GlobalDatabaseConnectingString;
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

    public void RegisterType(string Type, Action MapStart, Action<ResourceManifest> ServerPrecacheResources, Func<CCSPlayerController, Dictionary<string, string>, bool> Equip, Func<CCSPlayerController, Dictionary<string, string>, bool> Unequip, bool Equipable, bool? Alive)
    {
        Item.RegisterType(Type, MapStart, ServerPrecacheResources, Equip, Unequip, Equipable, Alive);
    }
}