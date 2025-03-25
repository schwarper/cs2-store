using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_Tracer
{
    public static void OnPluginStart()
    {
        Item.RegisterType("tracer", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.IsAnyItemExistInType("tracer"))
            Instance.RegisterEventHandler<EventBulletImpact>(OnBulletImpact);
    }

    public static void OnMapStart() { }

    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        Item.GetItemsByType("tracer").ForEach(item => manifest.AddResource(item.Value["model"]));
    }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }

    private static HookResult OnBulletImpact(EventBulletImpact @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        Store_Equipment? playertracer = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "tracer");
        if (playertracer == null)
            return HookResult.Continue;

        Dictionary<string, string>? itemdata = Item.GetItem(playertracer.UniqueId);
        if (itemdata == null)
            return HookResult.Continue;

        CBeam? entity = Utilities.CreateEntityByName<CBeam>("beam");
        if (entity == null || !entity.IsValid)
            return HookResult.Continue;

        string acceptinputvalue = itemdata.GetValueOrDefault("acceptInputValue", "Start");
        entity.SetModel(itemdata["model"]);
        entity.DispatchSpawn();
        entity.AcceptInput(acceptinputvalue);

        Vector position = Vec.GetEyePosition(player);
        entity.Teleport(position, new QAngle(), new Vector());

        entity.EndPos.X = @event.X;
        entity.EndPos.Y = @event.Y;
        entity.EndPos.Z = @event.Z;
        Utilities.SetStateChanged(entity, "CBeam", "m_vecEndPos");

        float lifetime = itemdata.TryGetValue("lifetime", out string? value) && float.TryParse(value, CultureInfo.InvariantCulture, out float lt) ? lt : 0.3f;

        Instance.AddTimer(lifetime, () =>
        {
            if (entity.IsValid)
                entity.Remove();
        });

        return HookResult.Continue;
    }
}