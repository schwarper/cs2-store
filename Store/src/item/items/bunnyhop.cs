using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

[StoreItemType("bunnyhop")]
public class ItemBunnyhop : IItemModule
{
    public bool Equipable => true;
    public bool? RequiresAlive => null;

    private static bool _bunnyhopExists;

    public void OnPluginStart()
    {
        _bunnyhopExists = Item.IsAnyItemExistInType("bunnyhop");
    }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest) { }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }

    public static void OnTick(CCSPlayerController player)
    {
        if (!_bunnyhopExists) return;

        StoreEquipment? playerBunnyhop = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamId == player.SteamID && p.Type == "bunnyhop");
        if (playerBunnyhop == null) return;

        if (player.PlayerPawn.Value is not { } playerPawn) return;

        playerPawn.BunnyHop(player);
    }
}