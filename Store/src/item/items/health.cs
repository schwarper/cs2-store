using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using static Store.Config_Config;
using static StoreApi.Store;

namespace Store;

[StoreItemType("health")]
public class Item_Health : IItemModule
{
    public bool Equipable => false;
    public bool? RequiresAlive => true;

    public void OnPluginStart() { }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest) { }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!int.TryParse(item["healthValue"], out int healthValue)) 
            return false;

        if (player.PlayerPawn?.Value is not { } playerPawn) 
            return false;

        int currentHealth = playerPawn.GetHealth();
        int maxHealth = Config.Settings.MaxHealth;
        int pawnMaxHealth = playerPawn.MaxHealth;

        if (maxHealth > 0 && currentHealth >= maxHealth) 
            return false;
        else if (maxHealth == -1 && currentHealth >= pawnMaxHealth) 
            return false;

        int newHealth = currentHealth + healthValue;

        if (maxHealth > 0)
        {
            newHealth = Math.Min(newHealth, maxHealth);
        }
        else if (maxHealth == -1)
        {
            newHealth = Math.Min(newHealth, pawnMaxHealth);
        }

        player.SetHealth(newHealth);
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}