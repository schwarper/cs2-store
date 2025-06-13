using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using System.Drawing;
using System.Globalization;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_Trail
{
    private static readonly Vector[] GlobalTrailLastOrigin = new Vector[64];
    private static readonly Vector[] GlobalTrailEndOrigin = new Vector[64];
    public static HashSet<CCSPlayerController> HideTrailPlayerList { get; set; } = [];
    public static readonly Dictionary<CEntityInstance, CCSPlayerController> TrailList = [];
    private static bool trailExists = false;

    public static void OnPluginStart()
    {
        Item.RegisterType("trail", OnMapStart, ServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.IsAnyItemExistInType("trail"))
        {
            trailExists = true;

            for (int i = 0; i < 64; i++)
            {
                GlobalTrailLastOrigin[i] = new();
                GlobalTrailEndOrigin[i] = new();
            }

            foreach (string command in Config.Commands.HideTrails)
                Instance.AddCommand(command, "Hide trails", Command_HideTrails);
        }
    }

    public static void OnMapStart() { }

    public static void ServerPrecacheResources(ResourceManifest manifest)
    {
        Item.GetItemsByType("trail")
            .Where(item => item.Value.TryGetValue("model", out string? model) && !string.IsNullOrEmpty(model))
            .ToList()
            .ForEach(item => manifest.AddResource(item.Value["model"]));
    }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }

    public static void OnTick(CCSPlayerController player)
    {
        if (!trailExists)
            return;

        Store_Equipment? playertrail = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "trail");
        if (playertrail == null)
            return;

        Dictionary<string, string>? itemdata = Item.GetItem(playertrail.UniqueId);
        if (itemdata == null)
            return;

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null || playerPawn.AbsOrigin == null)
            return;

        Vector absorigin = playerPawn.AbsOrigin;
        if (VectorExtensions.CalculateDistance(GlobalTrailLastOrigin[player.Slot], absorigin) <= 5.0f)
            return;

        VectorExtensions.Copy(absorigin, GlobalTrailLastOrigin[player.Slot]);

        float lifetime = itemdata.TryGetValue("lifetime", out string? ltvalue) && float.TryParse(ltvalue, CultureInfo.InvariantCulture, out float lt) ? lt : 1.3f;

        if (itemdata.TryGetValue("color", out string? cvalue))
        {
            Color? color = null;

            if (!string.IsNullOrEmpty(cvalue))
            {
                string[] colorValues = cvalue.Split(' ');
                color = Color.FromArgb(int.Parse(colorValues[0]), int.Parse(colorValues[1]), int.Parse(colorValues[2]));
            }

            CreateBeam(player, absorigin, lifetime, color, itemdata);
        }
        else
        {
            CreateParticle(absorigin, itemdata["model"], lifetime, itemdata, player);
        }
    }

    public static void CreateParticle(Vector absOrigin, string effectName, float lifetime, Dictionary<string, string> itemdata, CCSPlayerController player)
    {
        CParticleSystem? entity = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
        if (entity == null || !entity.IsValid)
            return;

        string acceptinputvalue = itemdata.GetValueOrDefault("acceptInputValue", "Start");
        QAngle angle = ParseAngle(itemdata.GetValueOrDefault("angleValue", "90 90 90"));

        entity.EffectName = effectName;
        entity.DispatchSpawn();
        entity.Teleport(absOrigin, angle, new Vector());
        entity.AcceptInput(acceptinputvalue);
        entity.AcceptInput("FollowEntity", player.PlayerPawn?.Value!, player.PlayerPawn?.Value!, "!activator");

        TrailList[entity] = player;

        Instance.AddTimer(lifetime, () =>
        {
            if (entity.IsValid)
                entity.Remove();
            TrailList.Remove(entity);
        });
    }

    public static void CreateBeam(CCSPlayerController player, Vector absOrigin, float lifetime, Color? color, Dictionary<string, string> itemdata)
    {
        CBeam? beam = Utilities.CreateEntityByName<CBeam>("env_beam");
        if (beam == null)
            return;

        if (VectorExtensions.IsZero(GlobalTrailEndOrigin[player.Slot]))
            VectorExtensions.Copy(absOrigin, GlobalTrailEndOrigin[player.Slot]);

        color ??= GetRandomColor();
        if (color == null)
            return;

        beam.RenderMode = RenderMode_t.kRenderTransColor;
        beam.Width = itemdata.TryGetValue("width", out string? widthValue) && float.TryParse(widthValue, CultureInfo.InvariantCulture, out float width) ? width : 1.0f;
        beam.Render = (Color)color;

        beam.Teleport(absOrigin, new QAngle(), new Vector());
        VectorExtensions.Copy(GlobalTrailEndOrigin[player.Slot], beam.EndPos);
        VectorExtensions.Copy(absOrigin, GlobalTrailEndOrigin[player.Slot]);

        Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");

        TrailList[beam] = player;

        Instance.AddTimer(lifetime, () =>
        {
            if (beam.IsValid)
                beam.Remove();
            TrailList.Remove(beam);
        });
    }

    private static Color? GetRandomColor()
    {
        KnownColor? randomColorName = (KnownColor?)Enum.GetValues(typeof(KnownColor)).GetValue(Instance.Random.Next(Enum.GetValues(typeof(KnownColor)).Length));
        return randomColorName.HasValue ? Color.FromKnownColor(randomColorName.Value) : null;
    }

    private static QAngle ParseAngle(string angleValue)
    {
        string[] angleValues = angleValue.Split(' ');
        return new QAngle(int.Parse(angleValues[0]), int.Parse(angleValues[1]), int.Parse(angleValues[2]));
    }

    public static void Command_HideTrails(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (!HideTrailPlayerList.Remove(player))
        {
            HideTrailPlayerList.Add(player);
            player.PrintToChatMessage("Hidetrails on");
        }
        else
        {
            player.PrintToChatMessage("Hidetrails off");
        }
    }
}