using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using static StoreApi.Store;

namespace StoreApi;

public interface IStoreApi
{
    public static readonly PluginCapability<IStoreApi?> Capability = new("store:api");

    public int GetPlayerCredits(CCSPlayerController player);
    public void SetPlayerCredits(CCSPlayerController player, int value);
    public void GivePlayerCredits(CCSPlayerController player, int amount);
    public bool Item_Purchase(CCSPlayerController player, Store_Item item);
    public bool Item_Equip(CCSPlayerController player, Store_Item item);
    public bool Item_Unequip(CCSPlayerController player, Store_Item item);
    public bool Item_Sell(CCSPlayerController player, Store_Item item);
    public bool Item_PlayerHas(CCSPlayerController player, string uniqueId);
    public bool Item_PlayerUsing(CCSPlayerController player, string uniqueId);
    public bool Item_IsInJson(string type, string uniqueId);
    public Store_Item? GetItem(string uniqueId);
    public List<Store_PlayerItem> GetPlayerItems(CCSPlayerController player);
    public List<Store_PlayerItem> GetPlayerEquipments(CCSPlayerController player);
    public bool IsPlayerVip(CCSPlayerController player);
    public void RegisterType(string type, Action mapStart, Func<CCSPlayerController, Store_Item, bool> equip, Func<CCSPlayerController, Store_Item, bool> unequip, bool equipable, bool? alive = false);

}