using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Globalization;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_Trail
{
    private static readonly Vector[] GlobalTrailLastOrigin = new Vector[64];
    private static readonly Vector[] GlobalTrailEndOrigin = new Vector[64];
    private static bool trailExists = false;

    public static void OnPluginStart()
    {
        Item.RegisterType("trail", OnMapStart, ServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.GetItemsByType("trail").Count > 0)
        {
            trailExists = true;

            for (int i = 0; i < 64; i++)
            {
                GlobalTrailLastOrigin[i] = new();
                GlobalTrailEndOrigin[i] = new();
            }
        }
    }
    public static void OnMapStart()
    {
    }
    public static void ServerPrecacheResources(ResourceManifest manifest)
    {
        List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("trail");

        foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
        {
            if (item.Value["uniqueid"].Contains(".vpcf"))
            {
                manifest.AddResource(item.Value["uniqueid"]);
            }
        }
    }
    public static void ServerPrecacheResources()
    {
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
        {
            return;
        }

        Store_Equipment? playertrail = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "trail");

        if (playertrail == null)
        {
            return;
        }

        Dictionary<string, string>? itemdata = Item.GetItem(playertrail.UniqueId);

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
                CreateBeam(player, absorigin, lifetime, null, itemdata);
            }
            else
            {
                string[] colorValues = itemdata["color"].Split(' ');

                Color color = Color.FromArgb(int.Parse(colorValues[0]), int.Parse(colorValues[1]), int.Parse(colorValues[2]));

                CreateBeam(player, absorigin, lifetime, color, itemdata);
            }
        }
        else
        {
            CreateParticle(absorigin, playertrail.UniqueId, lifetime, itemdata, player);
        }
    }

    public static void CreateParticle(Vector absOrigin, string effectName, float lifetime, Dictionary<string, string> itemdata, CCSPlayerController player)
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
        entity.AcceptInput("FollowEntity", player.PlayerPawn?.Value!, player.PlayerPawn?.Value!, "!activator");

        Instance.AddTimer(lifetime, () =>
        {
            if (entity != null && entity.IsValid)
            {
                entity.Remove();
            }
        });
    }

    public static void CreateBeam(CCSPlayerController player, Vector absOrigin, float lifetime, Color? color, Dictionary<string, string> itemdata)
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
            KnownColor? randomColorName = (KnownColor?)Enum.GetValues(typeof(KnownColor)).GetValue(Instance.Random.Next(Enum.GetValues(typeof(KnownColor)).Length));

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