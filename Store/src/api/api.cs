using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using MySqlConnector;
using StoreApi;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public class StoreAPI : IStoreApi
{
    public StoreAPI()
    {
    }

    public void SetPlayerCredits(CCSPlayerController player, int amount)
    {
        Credits.Set(player, amount);
    }
    
    public void GivePlayerCredits(CCSPlayerController player, int amount)
    {
        Credits.Give(player, amount);
    }
    
    public int GetPlayerCredits(CCSPlayerController player)
    {
        return Credits.Get(player);
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
    
    public Store_Item? GetItem(string UniqueId)
    {
        return Item.FindItemByUniqueId(UniqueId);
    }
    
    public bool PlayerHasItem(CCSPlayerController player, string UniqueId)
    {
        return Item.PlayerHas(player, UniqueId);
    }
    
    public bool PlayerUsingItem(CCSPlayerController player, string UniqueId)
    {
        return Item.PlayerUsing(player, UniqueId);
    }
    
    // public MySqlConnection DatabaseConnect()
    // {
    //     return Database.Connect();
    // }
    
    public bool ItemIsInJson(string type, string UniqueId)
    {
        return Item.IsInJson(type, UniqueId);
    }
}