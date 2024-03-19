using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    private void Health_OnPluginStart()
    {
        new StoreAPI().RegisterType("health", Health_OnMapStart, Health_OnEquip, Health_OnUnequip, false, true);
    }
    private void Health_OnMapStart()
    {
    }
    private bool Health_OnEquip(CCSPlayerController player, Store_Item item)
    {
        if (!int.TryParse(item.UniqueId, out int health))
        {
            return false;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
        {
            return false;
        }

        if (!int.TryParse(Instance.Config.Settings["max_health"], out int maxhealth))
        {
            return false;
        }

        int currentHealth = playerPawn.GetHealth();

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

        player.SetHealth(currentHealth + health);

        return true;
    }

    private bool Health_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
}