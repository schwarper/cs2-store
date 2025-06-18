using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using static Store.ConfigConfig;
using static StoreApi.Store;

namespace Store;

[StoreItemType("armor")]
public class ItemArmor : IItemModule
{
    public bool Equipable => false;
    public bool? RequiresAlive => true;

    public void OnPluginStart() { }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest) { }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!int.TryParse(item["armorValue"], out int armor))
            return false;

        if (player.PlayerPawn.Value is not { } playerPawn)
            return false;

        int maxArmor = Config.Settings.MaxArmor;
        if (maxArmor > 0 && playerPawn.ArmorValue >= maxArmor)
            return false;

        playerPawn.GiveArmor(Math.Min(armor, maxArmor - playerPawn.ArmorValue));
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}