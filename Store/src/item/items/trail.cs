using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Globalization;
using static CounterStrikeSharp.API.Core.Listeners;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    static readonly Vector[] GlobalTrailLastOrigin = new Vector[64];
    static readonly Vector[] GlobalTrailEndOrigin = new Vector[64];

    public static void Trail_OnPluginStart()
    {
        new StoreAPI().RegisterType("trail", Trail_OnMapStart, Trail_OnEquip, Trail_OnUnequip, true, null);

        for (int i = 0; i < 64; i++)
        {
            GlobalTrailLastOrigin[i] = new();
            GlobalTrailEndOrigin[i] = new();
        }
    }
    public static void Trail_OnMapStart()
    {
        IEnumerable<string> playerTrails = Instance.Config.Items
        .SelectMany(wk => wk.Value)
        .Where(kvp => kvp.Value["type"] == "trail")
        .Select(kvp => kvp.Value["uniqueid"]);

        Instance.RegisterListener<OnServerPrecacheResources>((manifest) =>
        {
            foreach (string UniqueId in playerTrails)
            {
                if (!UniqueId.Contains(".vpcf"))
                {
                    continue;
                }

                manifest.AddResource(UniqueId);
            }
        });
    }
    public static bool Trail_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
    public static bool Trail_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
    public static void OnTick_CreateTrail(CCSPlayerController player)
    {
        Store_Equipment? playertrail = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "trail");

        if (playertrail == null)
        {
            return;
        }

        Dictionary<string, string>? itemdata = Item.Find(playertrail.Type, playertrail.UniqueId);

        if (itemdata == null)
        {
            return;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
        {
            return;
        }

        Vector? absorigin = player.PlayerPawn.Value?.AbsOrigin;

        if (absorigin == null)
        {
            return;
        }

        if (Vec.CalculateDistance(GlobalTrailLastOrigin[player.Slot], absorigin) <= 5.0f)
        {
            return;
        }

        Vec.Copy(absorigin, GlobalTrailLastOrigin[player.Slot]);

        float lifetime = 1.3f;

        if (itemdata.TryGetValue("lifetime", out string? ltvalue) && float.TryParse(ltvalue, CultureInfo.InvariantCulture, out float lt))
        {
            lifetime = lt;
        }

        if (itemdata.TryGetValue("color", out string? cvalue))
        {
            if (string.IsNullOrEmpty(cvalue))
            {
                CreateTrail_Beam(player, absorigin, lifetime, null, itemdata);
            }
            else
            {
                string[] colorValues = itemdata["color"].Split(' ');

                Color color = Color.FromArgb(int.Parse(colorValues[0]), int.Parse(colorValues[1]), int.Parse(colorValues[2]));

                CreateTrail_Beam(player, absorigin, lifetime, color, itemdata);
            }
        }
        else
        {
            CreateTrail_Particle(absorigin, playertrail.UniqueId, lifetime, itemdata);
        }
    }

    public static void CreateTrail_Particle(Vector absOrigin, string effectName, float lifetime, Dictionary<string, string> itemdata)
    {
        CParticleSystem? entity = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");

        if (entity == null || !entity.IsValid)
        {
            return;
        }

        if (!itemdata.TryGetValue("acceptInputValue", out string? acceptinputvalue) || string.IsNullOrEmpty(acceptinputvalue))
        {
            acceptinputvalue = "Start";
        }

        QAngle angle = new();

        if (!itemdata.TryGetValue("angleValue", out string? angleValue) || string.IsNullOrEmpty(angleValue))
        {
            angle.X = 90;
        }
        else
        {
            string[] angleValues = angleValue.Split(' ');

            angle.X = int.Parse(angleValues[0]);
            angle.Y = int.Parse(angleValues[0]);
            angle.Z = int.Parse(angleValues[0]);
        }

        entity.EffectName = effectName;
        entity.DispatchSpawn();
        entity.Teleport(absOrigin, angle, new Vector());
        entity.AcceptInput(acceptinputvalue!);

        Instance.AddTimer(lifetime, () =>
        {
            if (entity != null && entity.IsValid)
            {
                entity.Remove();
            }
        });
    }

    public static void CreateTrail_Beam(CCSPlayerController player, Vector absOrigin, float lifetime, Color? color, Dictionary<string, string> itemdata)
    {
        CBeam? beam = Utilities.CreateEntityByName<CBeam>("env_beam");

        if (beam == null)
        {
            return;
        }

        if (Vec.IsZero(GlobalTrailEndOrigin[player.Slot]))
        {
            Vec.Copy(absOrigin, GlobalTrailEndOrigin[player.Slot]);
            return;
        }

        if (color == null)
        {
            KnownColor? randomColorName = (KnownColor?)Enum.GetValues(typeof(KnownColor)).GetValue(Instance.random.Next(Enum.GetValues(typeof(KnownColor)).Length));

            if (!randomColorName.HasValue)
            {
                return;
            }

            color = Color.FromKnownColor(randomColorName.Value);
        }

        float width = 1.0f;

        if (itemdata.TryGetValue("lifetime", out string? ltvalue) && float.TryParse(ltvalue, CultureInfo.InvariantCulture, out float fwidth))
        {
            width = fwidth;
        }

        beam.RenderMode = RenderMode_t.kRenderTransColor;
        beam.Width = width;
        beam.Render = (Color)color;

        beam.Teleport(absOrigin, new QAngle(), new Vector());

        Vec.Copy(GlobalTrailEndOrigin[player.Slot], beam.EndPos);
        Vec.Copy(absOrigin, GlobalTrailEndOrigin[player.Slot]);

        Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");

        Instance.AddTimer(lifetime, () =>
        {
            if (beam != null && beam.DesignerName == "env_beam")
            {
                beam.Remove();
            }
        });
    }
}