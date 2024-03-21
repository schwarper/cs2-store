using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using StoreApi;
using static StoreApi.Store;

namespace Store_TestModule;

public class Store_TestModule : BasePlugin
{
    public override string ModuleName { get; } = "Store Module [Test]";
    public override string ModuleVersion { get; } = "0.0.1";

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        var storeApi = IStoreApi.Capability.Get();

        if (storeApi == null)
        {
            return;
        }

        /*
         * Type
         * OnMapStart
         * Equip
         * Unequip
         * Equipable (If the item is equipable make this true, if not make it false.)
         * Alive (If the item is equipable, make it null. If not, make it true if it can be picked up alive, false if it can be picked up dead, or null if it doesn't matter.)
        */
        storeApi.RegisterType("test", OnMapStart, Equip, Unequip, true, true);
    }

    private void OnMapStart()
    {
    }

    private bool Equip(CCSPlayerController player, Store_Item item)
    {
        Server.PrintToChatAll($"Player {player.PlayerName} equipped {item.Name}");
        return true;
    }

    private bool Unequip(CCSPlayerController player, Store_Item item)
    {
        Server.PrintToChatAll($"Player {player.PlayerName} unequipped {item.Name}");
        return true;
    }
}