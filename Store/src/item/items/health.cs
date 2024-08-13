using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Store;

namespace Store;

public static class Item_Health
{
    public static void OnPluginStart()
    {
        Item.RegisterType("health", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, false, true);
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!int.TryParse(item["healthValue"], out int health))
        {
            return false;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
        {
            return false;
        }

        int maxhealth = Instance.Config.Settings.MaxHealth;

        int currentHealth = playerPawn.GetHealth();
        int pawnMaxHealth = playerPawn.MaxHealth;

        if (maxhealth > 0)
        {
            if (currentHealth >= maxhealth)
            {
                return false;
            }

            if (currentHealth + health > maxhealth)
            {
                health = maxhealth - currentHealth;
            }
        }

        if (maxhealth == -1)
        {
            if (currentHealth >= pawnMaxHealth)
            {
                return false;
            }

            if (currentHealth + health > pawnMaxHealth)
            {
                health = pawnMaxHealth - currentHealth;
            }
        }

        player.SetHealth(currentHealth + health);

        return true;
    }

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}