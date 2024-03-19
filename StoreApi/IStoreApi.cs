using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using static StoreApi.Store;

namespace StoreApi;

public interface IStoreApi
{
    public static readonly PluginCapability<IStoreApi?> Capability = new("store:api");

    public void SetPlayerCredits(CCSPlayerController player, int value);
    public void GivePlayerCredits(CCSPlayerController player, int amount);
    public int GetPlayerCredits(CCSPlayerController player);
    public bool IsPlayerVip(CCSPlayerController player);
    public void RegisterType(string type, Action mapStart, Func<CCSPlayerController, Store_Item, bool> equip, Func<CCSPlayerController, Store_Item, bool> unequip, bool equipable, bool? alive = false);
    public Store_Item? GetItem(string uniqueId);
    public bool PlayerHasItem(CCSPlayerController player, string uniqueId);
    public bool PlayerUsingItem(CCSPlayerController player, string uniqueId);
    public bool ItemIsInJson(string type, string uniqueId);
}