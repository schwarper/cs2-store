using System.Globalization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using static Store.ConfigConfig;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

[StoreItemType("trail")]
public class ItemTrail : IItemModule
{
    private static readonly Vector[] GlobalTrailLastOrigin = new Vector[64];
    private static readonly Vector[] GlobalTrailEndOrigin = new Vector[64];
    public static HashSet<CCSPlayerController> HideTrailPlayerList { get; set; } = [];
    public static readonly Dictionary<CEntityInstance, CCSPlayerController> TrailList = [];
    private static bool _trailExists;

    public bool Equipable => true;
    public bool? RequiresAlive => null;

    public void OnPluginStart()
    {
        if (Item.IsAnyItemExistInType("trail"))
        {
            _trailExists = true;

            for (int i = 0; i < 64; i++)
            {
                GlobalTrailLastOrigin[i] = new Vector();
                GlobalTrailEndOrigin[i] = new Vector();
            }

            foreach (string command in Config.Commands.HideTrails)
                Instance.AddCommand(command, "Hide trails", Command_HideTrails);
        }
    }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        Item.GetItemsByType("trail")
            .Where(item => item.Value.TryGetValue("model", out string? model) && !string.IsNullOrEmpty(model))
            .ToList()
            .ForEach(item => manifest.AddResource(item.Value["model"]));
    }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }

    public static void OnTick(CCSPlayerController player)
    {
        if (!_trailExists)
            return;

        StoreEquipment? playertrail = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamId == player.SteamID && p.Type == "trail");
        if (playertrail == null)
            return;

        var itemdata = Item.GetItem(playertrail.UniqueId);
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
        string acceptInputValue = itemdata.TryGetValue("acceptInputValue", out string? value) && !string.IsNullOrEmpty(value) ? value : "Start";
        
        CBaseEntity? trail = itemdata["entityType"] switch
        {
            "particle" => player.CreateFollowingParticle(itemdata["model"], acceptInputValue),
            "beam" => player.CreateFollowingBeam(float.Parse(itemdata["width"], CultureInfo.InvariantCulture), itemdata["color"], null),
            _ => throw new NotImplementedException()
        };

        if (trail == null)
            return;

        if (trail is CBeam beam)
        {
            if (VectorExtensions.IsZero(GlobalTrailEndOrigin[player.Slot]))
                VectorExtensions.Copy(absorigin, GlobalTrailEndOrigin[player.Slot]);
            
            VectorExtensions.Copy(GlobalTrailEndOrigin[player.Slot], beam.EndPos);
            VectorExtensions.Copy(absorigin, GlobalTrailEndOrigin[player.Slot]);
        }
        
        TrailList[trail] = player;

        Instance.AddTimer(lifetime, () =>
        {
            if (trail.IsValid)
                trail.Remove();
            TrailList.Remove(trail);
        });
    }

    private static void Command_HideTrails(CCSPlayerController? player, CommandInfo info)
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