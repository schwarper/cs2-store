using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using StoreApi;
using static Store.Store;
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

    public bool Item_Purchase(CCSPlayerController player, Store_Item item)
    {
        return Item.Purchase(player, item);
    }

    public bool Item_Equip(CCSPlayerController player, Store_Item item)
    {
        return Item.Equip(player, item);
    }

    public bool Item_Unequip(CCSPlayerController player, Store_Item item)
    {
        return Item.Unequip(player, item);
    }

    public bool Item_Sell(CCSPlayerController player, Store_Item item)
    {
        return Item.Sell(player, item);
    }

    public bool Item_PlayerHas(CCSPlayerController player, string UniqueId)
    {
        return Item.PlayerHas(player, UniqueId);
    }

    public bool Item_PlayerUsing(CCSPlayerController player, string UniqueId)
    {
        return Item.PlayerUsing(player, UniqueId);
    }

    public bool Item_IsInJson(string type, string UniqueId)
    {
        return Item.IsInJson(type, UniqueId);
    }

    public Store_Item? GetItem(string UniqueId)
    {
        return Item.FindItemByUniqueId(UniqueId);
    }

    public List<Store_PlayerItem> GetPlayerItems(CCSPlayerController player)
    {
        return Item.GetPlayerItems(player);
    }

    public List<Store_PlayerItem> GetPlayerEquipments(CCSPlayerController player)
    {
        return Item.GetPlayerEquipments(player);
    }

    public bool IsPlayerVip(CCSPlayerController player)
    {
        string vip = Instance.Config.Menu["vip_flag"];

        return !string.IsNullOrEmpty(vip) && AdminManager.PlayerHasPermissions(player, vip);
    }

    public void RegisterType(string Type, Action MapStart, Func<CCSPlayerController, Store_Item, bool> Equip, Func<CCSPlayerController, Store_Item, bool> Unequip, bool Equipable, bool? Alive)
    {
        Instance.GlobalStoreItemTypes.Add(new Store_Item_Types
        {
            Type = Type,
            MapStart = MapStart,
            Equip = Equip,
            Unequip = Unequip,
            Equipable = Equipable,
            Alive = Alive
        });
    }
}