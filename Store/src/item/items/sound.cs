using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public static void Sound_OnPluginStart()
    {
        new StoreAPI().RegisterType("sound", Sound_OnMapStart, Sound_OnEquip, Sound_OnUnequip, false, null);
    }
    public static void Sound_OnMapStart()
    {
        IEnumerable<string> playerSkinItems = Instance.Config.Items
        .SelectMany(wk => wk.Value)
        .Where(kvp => kvp.Value.Type == "playerskin")
        .Select(kvp => kvp.Value.UniqueId);

        Instance.RegisterListener<OnServerPrecacheResources>((manifest) =>
        {
            foreach (string UniqueId in playerSkinItems)
            {
                manifest.AddResource(UniqueId);
            }
        });
    }
    public static bool Sound_OnEquip(CCSPlayerController player, Store_Item item)
    {
        foreach (CCSPlayerController target in Utilities.GetPlayers())
        {
            if (target == null || !target.IsValid)
            {
                continue;
            }

            target.ExecuteClientCommand($"play {item.UniqueId}");
        }

        return true;
    }
    public static bool Sound_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
}