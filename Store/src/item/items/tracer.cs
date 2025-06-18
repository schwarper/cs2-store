using System.Globalization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

[StoreItemType("tracer")]
public class ItemTracer : IItemModule
{
    public bool Equipable => true;
    public bool? RequiresAlive => null;

    public void OnPluginStart()
    {
        if (Item.IsAnyItemExistInType("tracer"))
            Instance.RegisterEventHandler<EventBulletImpact>(OnBulletImpact);
    }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        Item.GetItemsByType("tracer").ForEach(item => manifest.AddResource(item.Value["model"]));
    }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }

    private static HookResult OnBulletImpact(EventBulletImpact @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        StoreEquipment? playertracer = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamId == player.SteamID && p.Type == "tracer");
        if (playertracer == null)
            return HookResult.Continue;

        var itemdata = Item.GetItem(playertracer.UniqueId);
        if (itemdata == null)
            return HookResult.Continue;

        CBeam? entity = player.CreateFollowingBeam(int.Parse(itemdata["width"], CultureInfo.InvariantCulture), itemdata["color"], new Vector(@event.X, @event.Y, @event.Z), true);
        if (entity == null)
            return HookResult.Continue;

        float lifetime = itemdata.TryGetValue("lifetime", out string? value) && float.TryParse(value, CultureInfo.InvariantCulture, out float lt) ? lt : 0.3f;

        Instance.AddTimer(lifetime, () =>
        {
            if (entity.IsValid)
                entity.Remove();
        });

        return HookResult.Continue;
    }
}