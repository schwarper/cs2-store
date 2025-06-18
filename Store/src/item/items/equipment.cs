using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

[StoreItemType("equipment")]
public class ItemEquipment : IItemModule
{
    public bool Equipable => true;
    public bool? RequiresAlive => null;

    private static readonly Dictionary<CCSPlayerController, Dictionary<int, CDynamicProp>> PlayerEquipmentEntities = [];

    public void OnPluginStart()
    {
        if (!Item.IsAnyItemExistInType("equipment")) return;
        
        Instance.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        Instance.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    }

    public void OnMapStart()
    {
        PlayerEquipmentEntities.Clear();
    }

    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        var items = Item.GetItemsByType("equipment");

        foreach (var item in items)
            manifest.AddResource(item.Value["model"]);
    }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!item.TryGetValue("slot", out string? slotStr) || !int.TryParse(slotStr, out int slot) || slot < 0)
            return false;

        EquipModel(player, item["model"], slot);
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        if (!item.TryGetValue("slot", out string? slotStr) || !int.TryParse(slotStr, out int slot))
            return false;

        UnEquipModel(player, slot);
        return true;
    }

    private static void EquipModel(CCSPlayerController player, string model, int slot)
    {
        UnEquipModel(player, slot);

        Server.NextFrame(() =>
        {
            CDynamicProp? entity = CreateItem(player, model);
            if (entity == null || !entity.IsValid) return;
            
            if (!PlayerEquipmentEntities.ContainsKey(player))
                PlayerEquipmentEntities[player] = [];

            PlayerEquipmentEntities[player][slot] = entity;
        });
    }

    private static void UnEquipModel(CCSPlayerController player, int slot)
    {
        if (!PlayerEquipmentEntities.TryGetValue(player, out var value) || !value.TryGetValue(slot, out CDynamicProp? entity))
            return;
        
        if (entity.IsValid)
            entity.Remove();

        value.Remove(slot);

        if (value.Count == 0)
            PlayerEquipmentEntities.Remove(player);
    }

    private static CDynamicProp? CreateItem(CCSPlayerController player, string model)
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

    private static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null) return HookResult.Continue;

        List<StoreEquipment> equippedItems = [.. Instance.GlobalStorePlayerEquipments.Where(x => x.SteamId == player.SteamID && x.Type == "equipment")];

        if (PlayerEquipmentEntities.TryGetValue(player, out var value))
        {
            List<int> slotsToRemove = [.. value.Where(kv => equippedItems.All(item => item.Slot != kv.Key)).Select(kv => kv.Key)];

            foreach (int slot in slotsToRemove)
                UnEquipModel(player, slot);
        }

        foreach (StoreEquipment? item in equippedItems)
        {
            if (Item.GetItem(item.UniqueId) is not { } itemData ||
                !itemData.TryGetValue("model", out string? model) ||
                !itemData.TryGetValue("slot", out string? slotStr) || !int.TryParse(slotStr, out int slot)) continue;
            
            if (PlayerEquipmentEntities.TryGetValue(player, out var equipment) &&
                equipment.TryGetValue(slot, out CDynamicProp? entity) && entity.IsValid)
                continue;

            EquipModel(player, model, slot);
        }

        return HookResult.Continue;
    }

    private static HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        CleanUpModels(@event.Userid);
        return HookResult.Continue;
    }

    private static HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CleanUpModels(@event.Userid);
        return HookResult.Continue;
    }

    private static void CleanUpModels(CCSPlayerController? player)
    {
        if (player == null || !PlayerEquipmentEntities.TryGetValue(player, out var value)) return;
        
        foreach (CDynamicProp entity in value.Values.Where(entity => entity.IsValid))
            entity.Remove();

        PlayerEquipmentEntities.Remove(player);
    }
}