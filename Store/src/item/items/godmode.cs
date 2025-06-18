using System.Globalization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

[StoreItemType("godmode")]
public class ItemGodmode : IItemModule
{
    public bool Equipable => false;
    public bool? RequiresAlive => true;

    public void OnPluginStart() { }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest) { }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!float.TryParse(item["godmodeTimerValue"], CultureInfo.InvariantCulture, out float godmodeTimerValue))
        {
            return false;
        }

        if (player.PlayerPawn.Value is not { } playerPawn) return false;

        if (godmodeTimerValue > 0.0f)
        {
            Instance.AddTimer(godmodeTimerValue, () =>
            {
                playerPawn.TakesDamage = true;
                player.PrintToChatMessage("Godmode expired");
            });
        }

        playerPawn.TakesDamage = false;
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}