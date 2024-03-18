using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Store;

public partial class Store : BasePlugin
{
    private void Gravity_OnPluginStart()
    {
        new StoreAPI().RegisterType("gravity", Gravity_OnMapStart, Gravity_OnEquip, Gravity_OnUnequip, false, true);
    }
    private void Gravity_OnMapStart()
    {
    }
    private bool Gravity_OnEquip(CCSPlayerController player, Store_Item item)
    {
        if (!float.TryParse(item.UniqueId, out float gravity))
        {
            return false;
        }

        player.GravityScale = gravity;

        Utilities.SetStateChanged(player, "CBaseEntity", "m_flGravityScale");

        return true;
    }
    private bool Gravity_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
}