using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static StoreApi.Store;

namespace Store;

[StoreItemType("open")]
public class Item_Open : IItemModule
{
    private static readonly string[] DoorNames =
    [
        "func_door",
        "func_movelinear",
        "func_door_rotating",
        "prop_door_rotating"
    ];
    
    public bool Equipable => false;
    public bool? RequiresAlive => null;

    public void OnPluginStart() { }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest) { }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        foreach (string doorName in DoorNames)
        {
            IEnumerable<CBaseEntity> doors = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(doorName);
            foreach (CBaseEntity door in doors)
            {
                door.AcceptInput("Open");
            }
        }

        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}