using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_Equipment
{
    private readonly static Dictionary<ulong, CBaseModelEntity> Equipment = [];

    public static void OnPluginStart()
    {
        Item.RegisterType("equipment", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.IsAnyItemExistInType("equipment"))
        {
            Instance.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        }
    }
    public static void OnMapStart()
    {
    }
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
        if (!update)
        {
            return true;
        }

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

            Dictionary<string, string>? itemdata = Item.GetItem(playerItems.UniqueId);
            if (itemdata == null) return;

            player.PlayerPawn.Value!.Effects = 1;
            Utilities.SetStateChanged(player.PlayerPawn.Value!, "CBaseEntity", "m_fEffects");

            CreateItem(player, itemdata["model"]);
        });
    }
    public static void UnEquip(CCSPlayerController player)
    {
        if (Equipment.TryGetValue(player.SteamID, out CBaseModelEntity? entity))
        {
            if (entity.IsValid) entity.Remove();
            Equipment.Remove(player.SteamID);
        }
    }

    public static void CreateItem(CCSPlayerController player, string itemName)
    {
        CBaseModelEntity? entity = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic_override");

        Instance.AddTimer(0.1f, () =>
        {
            entity!.Globalname = $"{player.SteamID}({itemName})#{RandomString(6)}";
            entity.SetModel(itemName);
            entity.DispatchSpawn();
            entity.AcceptInput("FollowEntity", player.PlayerPawn?.Value!, player.PlayerPawn?.Value!, "!activator");
            Equipment[player.SteamID] = entity;
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
        char[] result = new char[length];
        for (int i = 0; i < length; i++)
            result[i] = chars[random.Next(chars.Length)];

        return new string(result);
    }
}