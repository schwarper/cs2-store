using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using System.Globalization;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

[StoreItemType("speed")]
public class Item_Speed : IItemModule
{
    public bool Equipable => false;
    public bool? RequiresAlive => true;
    
    public void OnPluginStart() { }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest) { }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!float.TryParse(item["speedValue"], CultureInfo.InvariantCulture, out float speed) ||
            !float.TryParse(item["speedTimerValue"], CultureInfo.InvariantCulture, out float speedtimer))
            return false;

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null)
            return false;

        playerPawn.VelocityModifier = speed;

        if (speedtimer > 0.0)
        {
            Instance.AddTimer(speedtimer, () =>
            {
                playerPawn.VelocityModifier = 1.0f;
                player.PrintToChatMessage("Speed expired");
            });
        }

        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}