using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Config_Config;

namespace Store;

public static class Item_Health
{
    public static void OnPluginStart() =>
        Item.RegisterType("health", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, false, true);

    public static void OnMapStart() { }

    public static void OnServerPrecacheResources(ResourceManifest manifest) { }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!int.TryParse(item["healthValue"], out int healthValue)) return false;

        if (player.PlayerPawn?.Value is not { } playerPawn) return false;

        int currentHealth = playerPawn.GetHealth();
        int maxHealth = Config.Settings.MaxHealth;
        int pawnMaxHealth = playerPawn.MaxHealth;

        if (maxHealth > 0 && currentHealth >= maxHealth) return false;
        else if (maxHealth == -1 && currentHealth >= pawnMaxHealth) return false;

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

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update) => true;
}