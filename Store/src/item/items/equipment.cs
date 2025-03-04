using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_Equipment
{
    private static readonly Dictionary<ulong, CBaseModelEntity> _equipment = [];

    public static void OnPluginStart()
    {
        Item.RegisterType("equipment", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.IsAnyItemExistInType("equipment"))
        {
            Instance.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        }
    }

    public static void OnMapStart() { }

    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("equipment");

        foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
        {
            manifest.AddResource(item.Value["model"]);
        }
    }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        Equip(player);
        return true;
    }

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        if (!update) return true;

        UnEquip(player);
        return true;
    }

    public static void Equip(CCSPlayerController player)
    {
        UnEquip(player);

        Instance.AddTimer(0.1f, () =>
        {
            Store_Equipment? playerItems = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "equipment");
            if (playerItems == null) return;

            Dictionary<string, string>? itemData = Item.GetItem(playerItems.UniqueId);
            if (itemData == null) return;

            if (player.PlayerPawn?.Value is not { } pawn) return;

            pawn.Effects = 1;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_fEffects");

            CreateItem(player, itemData["model"]);
        });
    }

    public static void UnEquip(CCSPlayerController player)
    {
        if (_equipment.TryGetValue(player.SteamID, out CBaseModelEntity? entity))
        {
            if (entity.IsValid) entity.Remove();
            _equipment.Remove(player.SteamID);
        }
    }

    public static void CreateItem(CCSPlayerController player, string itemName)
    {
        CBaseModelEntity? entity = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic_override");

        Instance.AddTimer(0.1f, () =>
        {
            if (entity == null) return;

            entity.Globalname = $"{player.SteamID}({itemName})#{RandomString(6)}";
            entity.SetModel(itemName);
            entity.DispatchSpawn();
            entity.AcceptInput("FollowEntity", player.PlayerPawn?.Value!, player.PlayerPawn?.Value!, "!activator");
            _equipment[player.SteamID] = entity;
        });
    }

    public static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        Equip(@event.Userid!);
        return HookResult.Continue;
    }

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        Random random = new();
        return new string([.. Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)])]);
    }
}