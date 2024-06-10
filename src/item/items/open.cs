using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Store;

public static class Item_Open
{
    private static readonly string[] DoorNames =
    [
        "func_door",
        "func_movelinear",
        "func_door_rotating",
        "prop_door_rotating"
    ];

    public static void OnPluginStart()
    {
        Item.RegisterType("open", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, false, null);
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        foreach (string doorname in DoorNames)
        {
            IEnumerable<CBaseEntity> target = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(doorname);

            foreach (CBaseEntity entity in target)
            {
                entity.AcceptInput("Open");
            }
        }

        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
}