using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Store;

namespace Store;

public static class Item_Equipment
{
    private static readonly Dictionary<CCSPlayerController, Dictionary<int, CDynamicProp>> PlayerEquipmentEntities = [];

    public static void OnPluginStart()
    {
        Item.RegisterType("equipment", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.IsAnyItemExistInType("equipment"))
        {
            Instance.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Instance.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
            Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        }
    }

    public static void OnMapStart()
    {
        PlayerEquipmentEntities.Clear();
    }

    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("equipment");

        foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
            manifest.AddResource(item.Value["model"]);
    }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!item.TryGetValue("slot", out string? slotStr) || !int.TryParse(slotStr, out int slot) || slot < 0)
            return false;

        EquipModel(player, item["model"], slot);

        return true;
    }

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        if (!item.TryGetValue("slot", out string? slotStr) || !int.TryParse(slotStr, out int slot))
            return false;

        UnEquipModel(player, slot);

        return true;
    }

    public static void EquipModel(CCSPlayerController player, string model, int slot)
    {
        UnEquipModel(player, slot);

        Server.NextFrame(() =>
        {
            CDynamicProp? entity = CreateItem(player, model);
            if (entity != null && entity.IsValid)
            {
                if (!PlayerEquipmentEntities.ContainsKey(player))
                    PlayerEquipmentEntities[player] = [];

                PlayerEquipmentEntities[player][slot] = entity;
            }
        });
    }

    public static void UnEquipModel(CCSPlayerController player, int slot)
    {
        if (!PlayerEquipmentEntities.TryGetValue(player, out Dictionary<int, CDynamicProp>? value) || !value.ContainsKey(slot))
            return;

        CDynamicProp? entity = value[slot];

        if (entity != null && entity.IsValid)
            entity.Remove();

        value.Remove(slot);

        if (value.Count == 0)
            PlayerEquipmentEntities.Remove(player);
    }

    public static CDynamicProp? CreateItem(CCSPlayerController player, string model)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null) return null;

        CDynamicProp? entity = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");

        if (entity == null)
            return null;

        entity.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
        entity.SetModel(model);
        entity.DispatchSpawn();
        entity.AcceptInput("FollowEntity", pawn, pawn, "!activator");

        return entity;
    }

    public static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null) return HookResult.Continue;

        List<StoreApi.Store.Store_Equipment> equippedItems = [.. Instance.GlobalStorePlayerEquipments.Where(x => x.SteamID == player.SteamID && x.Type == "equipment")];

        if (PlayerEquipmentEntities.TryGetValue(player, out Dictionary<int, CDynamicProp>? value))
        {
            List<int> slotsToRemove = [.. value.Where(kv => !equippedItems.Any(item => item.Slot == kv.Key)).Select(kv => kv.Key)];

            foreach (int slot in slotsToRemove)
                UnEquipModel(player, slot);
        }

        foreach (StoreApi.Store.Store_Equipment? item in equippedItems)
        {
            if (Item.GetItem(item.UniqueId) is Dictionary<string, string> itemData &&
                itemData.TryGetValue("model", out string? model) &&
                itemData.TryGetValue("slot", out string? slotStr) && int.TryParse(slotStr, out int slot))
            {
                if (PlayerEquipmentEntities.TryGetValue(player, out Dictionary<int, CDynamicProp>? equipment) &&
                    equipment.TryGetValue(slot, out CDynamicProp? entity) && entity != null && entity.IsValid)
                    continue;

                EquipModel(player, model, slot);
            }
        }

        return HookResult.Continue;
    }

    public static HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        CleanUpModels(@event.Userid);
        return HookResult.Continue;
    }

    public static HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CleanUpModels(@event.Userid);
        return HookResult.Continue;
    }

    public static void CleanUpModels(CCSPlayerController? player)
    {
        if (player != null && PlayerEquipmentEntities.TryGetValue(player, out Dictionary<int, CDynamicProp>? value))
        {
            foreach (CDynamicProp entity in value.Values)
                if (entity != null && entity.IsValid)
                    entity.Remove();

            PlayerEquipmentEntities.Remove(player);
        }
    }
}