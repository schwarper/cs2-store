using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static StoreApi.Store;

namespace Store;

[StoreItemType("respawn")]
public class ItemRespawn : IItemModule
{
    public bool Equipable => false;
    public bool? RequiresAlive => false;

    public void OnPluginStart() { }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest) { }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (player.Team is not (CsTeam.Terrorist or CsTeam.CounterTerrorist))
            return false;

        player.Respawn();
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}