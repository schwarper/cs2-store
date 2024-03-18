using CounterStrikeSharp.API.Core;

namespace Store;

public partial class Store : BasePlugin
{
    private void Speed_OnPluginStart()
    {
        new StoreAPI().RegisterType("speed", Speed_OnMapStart, Speed_OnEquip, Speed_OnUnequip, false, true);
    }
    private void Speed_OnMapStart()
    {
    }
    private bool Speed_OnEquip(CCSPlayerController player, Store_Item item)
    {
        if (!float.TryParse(item.UniqueId, out float speed))
        {
            return false;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
        {
            return false;
        }

        if (speed > 0.0)
        {
            AddTimer(speed, () =>
            {
                playerPawn.VelocityModifier = 1.0f;

                player.PrintToChatMessage("Speed expired");
            });
        }

        playerPawn.VelocityModifier = speed;

        return true;
    }
    private bool Speed_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
}