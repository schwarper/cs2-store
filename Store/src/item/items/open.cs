using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Store;

public partial class Store
{
    static readonly string[] DoorNames =
    {
        "func_door",
        "func_movelinear",
        "func_door_rotating",
        "prop_door_rotating"
    };

    public static void Open_OnPluginStart()
    {
        Item.RegisterType("open", Open_OnMapStart, Open_OnEquip, Open_OnUnequip, false, null);
    }
    public static void Open_OnMapStart()
    {
    }
    public static bool Open_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
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
    public static bool Open_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
}