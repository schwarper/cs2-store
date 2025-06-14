using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using static StoreApi.Store;

namespace Store;

[StoreItemType("gravity")]
public class Item_Gravity : IItemModule
{
    public bool Equipable => false;
    public bool? RequiresAlive => true;

    public void OnPluginStart() { }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest) { }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!float.TryParse(item["gravityValue"], CultureInfo.InvariantCulture, out float gravityValue))
        {
            return false;
        }

        if (player.PlayerPawn?.Value is not { } playerPawn) return false;

        playerPawn.GravityScale = gravityValue;
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        if (player.PlayerPawn?.Value is { } playerPawn)
        {
            playerPawn.GravityScale = 1.0f;
        }
        return true;
    }
}