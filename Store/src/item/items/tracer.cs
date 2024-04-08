using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using static CounterStrikeSharp.API.Core.Listeners;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public static void Tracer_OnPluginStart()
    {
        new StoreAPI().RegisterType("tracer", Tracer_OnMapStart, Tracer_OnEquip, Tracer_OnUnequip, true, null);

        Instance.RegisterEventHandler<EventBulletImpact>((@event, info) =>
        {
            CCSPlayerController? player = @event.Userid;

            if (player == null || !player.IsValid)
            {
                return HookResult.Continue;
            }

            Store_Equipment? playertracer = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "tracer");

            if (playertracer == null)
            {
                return HookResult.Continue;
            }

            Dictionary<string, string>? itemdata = Item.Find(playertracer.Type, playertracer.UniqueId);

            if (itemdata == null)
            {
                return HookResult.Continue;
            }

            CBeam? entity = Utilities.CreateEntityByName<CBeam>("beam");

            if (entity == null || !entity.IsValid)
            {
                return HookResult.Continue;
            }

            if (!itemdata.TryGetValue("acceptInputValue", out string? acceptinputvalue) || string.IsNullOrEmpty(acceptinputvalue))
            {
                acceptinputvalue = "Start";
            }

            entity.SetModel(playertracer.UniqueId);
            entity.DispatchSpawn();
            entity.AcceptInput(acceptinputvalue!);

            Vector position = Vec.GetEyePosition(player);

            entity.Teleport(position, new QAngle(), new Vector());

            entity.EndPos.X = @event.X;
            entity.EndPos.Y = @event.Y;
            entity.EndPos.Z = @event.Z;

            Utilities.SetStateChanged(entity, "CBeam", "m_vecEndPos");

            float lifetime = 0.3f;

            if (itemdata.TryGetValue("lifetime", out string? value) && float.TryParse(value, CultureInfo.InvariantCulture, out float lt))
            {
                lifetime = lt;
            }

            Instance.AddTimer(lifetime, () =>
            {
                if (entity != null && entity.IsValid)
                {
                    entity.Remove();
                }
            });

            return HookResult.Continue;
        });
    }
    public static void Tracer_OnMapStart()
    {
        IEnumerable<string> playerTracers = Instance.Config.Items
        .SelectMany(wk => wk.Value)
        .Where(kvp => kvp.Value["type"] == "trail")
        .Select(kvp => kvp.Value["uniqueid"]);

        Instance.RegisterListener<OnServerPrecacheResources>((manifest) =>
        {
            foreach (string UniqueId in playerTracers)
            {
                manifest.AddResource(UniqueId);
            }
        });
    }
    public static bool Tracer_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
    public static bool Tracer_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
}