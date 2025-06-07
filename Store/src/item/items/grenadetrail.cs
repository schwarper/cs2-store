using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_GrenadeTrail
{
    public static Dictionary<CBaseCSGrenadeProjectile, CParticleSystem> GlobalGrenadeTrail { get; set; } = [];
    private static bool _grenadeTrailExists = false;

    public static void OnPluginStart()
    {
        Item.RegisterType("grenadetrail", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);
        _grenadeTrailExists = Item.IsAnyItemExistInType("grenadetrail");
    }

    public static void OnMapStart() { }

    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("grenadetrail");

        foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
        {
            manifest.AddResource(item.Value["model"]);
        }
    }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
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

            Store_Equipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "grenadetrail");
            if (item == null) return;

            CParticleSystem? particle = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
            if (particle == null || !particle.IsValid) return;

            Dictionary<string, string>? itemData = Item.GetItem(item.UniqueId);
            if (itemData == null) return;

            string acceptInputValue = itemData.TryGetValue("acceptInputValue", out string? value) && !string.IsNullOrEmpty(value) ? value : "Start";

            particle.EffectName = itemData["model"];
            particle.StartActive = true;
            particle.Teleport(grenade.AbsOrigin);
            particle.DispatchSpawn();
            particle.AcceptInput("FollowEntity", grenade, particle, "!activator");
            particle.AcceptInput(acceptInputValue);

            GlobalGrenadeTrail[grenade] = particle;
        });
    }
}