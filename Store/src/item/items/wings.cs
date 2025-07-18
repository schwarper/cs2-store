using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

[StoreItemType("wings")]
public class Item_Wings : IItemModule
{
    public bool Equipable => true;
    public bool? RequiresAlive => null;

    private static readonly Dictionary<CCSPlayerController, Dictionary<int, CDynamicProp>> PlayerWingsEntities = [];

    public void OnPluginStart()
    {
        if (Item.IsAnyItemExistInType("wings"))
        {
            Instance.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Instance.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
            Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        }
    }

    public void OnMapStart()
    {
        PlayerWingsEntities.Clear();
    }

    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        var items = Item.GetItemsByType("wings");
        foreach (var item in items)
        {
            if (item.Value.TryGetValue("model", out var model) && !string.IsNullOrEmpty(model))
                manifest.AddResource(model);
        }
    }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!item.TryGetValue("slot", out var slotStr) || !int.TryParse(slotStr, out var slot) || slot < 0)
            return false;
        EquipWings(player, item["model"], slot);
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        if (!item.TryGetValue("slot", out var slotStr) || !int.TryParse(slotStr, out var slot))
            return false;
        UnEquipWings(player, slot);
        return true;
    }

    public static void EquipWings(CCSPlayerController player, string model, int slot)
    {
        UnEquipWings(player, slot);
        Server.NextFrame(() =>
        {
            var entity = CreateWings(player, model);
            if (entity != null && entity.IsValid)
            {
                if (!PlayerWingsEntities.ContainsKey(player))
                    PlayerWingsEntities[player] = [];
                PlayerWingsEntities[player][slot] = entity;
            }
        });
    }

    public static void UnEquipWings(CCSPlayerController player, int slot)
    {
        if (!PlayerWingsEntities.TryGetValue(player, out var value) || !value.ContainsKey(slot))
            return;
        var entity = value[slot];
        if (entity != null && entity.IsValid)
            entity.Remove();
        value.Remove(slot);
        if (value.Count == 0)
            PlayerWingsEntities.Remove(player);
    }

    public static CDynamicProp? CreateWings(CCSPlayerController player, string model)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return null;
        var entity = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (entity == null) return null;
        entity.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
        entity.SetModel(model);
        entity.DispatchSpawn();
        entity.AcceptInput("FollowEntity", pawn, pawn, "!activator");
        var origin = pawn.AbsOrigin;
        if (origin != null)
        {
            var offset = new Vector(0, 0, 0);
            entity.Teleport(origin + offset, pawn.EyeAngles, pawn.AbsVelocity);
        }
        return entity;
    }

    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;
        var wings = Item.GetPlayerEquipments(player, "wings");
        foreach (var equip in wings)
        {
            var item = Item.GetItem(equip.UniqueId);
            if (item != null && item.TryGetValue("model", out var model))
            {
                EquipWings(player, model, equip.Slot);
            }
        }
        return HookResult.Continue;
    }

    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;
        if (PlayerWingsEntities.ContainsKey(player))
        {
            foreach (var slot in PlayerWingsEntities[player].Keys.ToList())
                UnEquipWings(player, slot);
        }
        return HookResult.Continue;
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;
        if (PlayerWingsEntities.ContainsKey(player))
        {
            foreach (var slot in PlayerWingsEntities[player].Keys.ToList())
                UnEquipWings(player, slot);
        }
        return HookResult.Continue;
    }
} 