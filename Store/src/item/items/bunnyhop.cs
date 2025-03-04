using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_Bunnyhop
{
    private static bool _bunnyhopExists = false;

    public static void OnPluginStart()
    {
        Item.RegisterType("bunnyhop", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, false);

        _bunnyhopExists = Item.IsAnyItemExistInType("bunnyhop");
    }

    public static void OnMapStart() { }

    public static void OnServerPrecacheResources(ResourceManifest manifest) { }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item) => true;

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update) => true;

    public static void OnTick(CCSPlayerController player)
    {
        if (!_bunnyhopExists) return;

        Store_Equipment? playerBunnyhop = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "bunnyhop");
        if (playerBunnyhop == null) return;

        if (player.PlayerPawn?.Value is not { } playerPawn) return;

        playerPawn.BunnyHop(player);
    }
}