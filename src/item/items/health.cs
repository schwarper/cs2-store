using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Store;

public partial class Store : BasePlugin
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

        SetHealth(player, GetHealth(player) + health);

        return true;
    }
    private bool Health_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
    private static void SetHealth(CCSPlayerController player, int health)
    {
        if (player.PlayerPawn == null || player.PlayerPawn.Value == null)
        {
            return;
        }

        player.Health = health;
        player.PlayerPawn.Value.Health = health;

        if (health > 100)
        {
            player.MaxHealth = health;
            player.PlayerPawn.Value.MaxHealth = health;
        }

        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
    }
    private static int GetHealth(CCSPlayerController player)
    {
        return player.PlayerPawn?.Value?.Health ?? 0;
    }
}