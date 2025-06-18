using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

[StoreItemType("grenadetrail")]
public class ItemGrenadeTrail : IItemModule
{
    public bool Equipable => true;
    public bool? RequiresAlive => null;

    private static Dictionary<CBaseCSGrenadeProjectile, CBaseEntity> GlobalGrenadeTrail { get; set; } = [];
    private static bool _grenadeTrailExists;

    public void OnPluginStart()
    {
        _grenadeTrailExists = Item.IsAnyItemExistInType("grenadetrail");
    }

    public void OnMapStart()
    {
        GlobalGrenadeTrail.Clear();
    }

    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        var items = Item.GetItemsByType("grenadetrail");

        foreach (var item in items)
        {
            manifest.AddResource(item.Value["model"]);
        }
    }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }

    public static void OnEntityCreated(CEntityInstance entity)
    {
        if (!_grenadeTrailExists || !entity.DesignerName.EndsWith("_projectile")) return;

        CBaseCSGrenadeProjectile grenade = new(entity.Handle);
        if (grenade.Handle == IntPtr.Zero) return;

        Server.NextFrame(() =>
        {
            CBasePlayerController? player = grenade.Thrower.Value?.Controller.Value;
            if (player == null) return;

            StoreEquipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamId == player.SteamID && p.Type == "grenadetrail");
            if (item == null) return;
            
            var itemData = Item.GetItem(item.UniqueId);
            if (itemData == null) return;
            
            string acceptInputValue = itemData.TryGetValue("acceptInputValue", out string? value) && !string.IsNullOrEmpty(value) ? value : "Start";

            CBaseEntity? trail = itemData["entityType"] switch
            {
                "particle" => grenade.CreateFollowingParticle(itemData["model"], acceptInputValue),
                "beam" => grenade.CreateFollowingBeam(float.Parse(itemData["width"], CultureInfo.InvariantCulture), itemData["color"], null),
                _ => throw new NotImplementedException()
            };

            if (trail == null)
                return;

            GlobalGrenadeTrail[grenade] = trail;
        });
    }
}