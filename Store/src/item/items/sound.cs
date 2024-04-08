using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;

namespace Store;

public partial class Store
{
    public static void Sound_OnPluginStart()
    {
        new StoreAPI().RegisterType("sound", Sound_OnMapStart, Sound_OnEquip, Sound_OnUnequip, false, null);
    }
    public static void Sound_OnMapStart()
    {
        IEnumerable<string> sounds = Instance.Config.Items
        .SelectMany(wk => wk.Value)
        .Where(kvp => kvp.Value["type"] == "sound")
        .Select(kvp => kvp.Value["uniqueid"]);

        Instance.RegisterListener<OnServerPrecacheResources>((manifest) =>
        {
            foreach (string UniqueId in sounds)
            {
                if (!UniqueId.Contains(".vsnd"))
                {
                    continue;
                }

                manifest.AddResource(UniqueId);
            }
        });
    }
    public static bool Sound_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        foreach (CCSPlayerController target in Utilities.GetPlayers())
        {
            if (target == null || !target.IsValid)
            {
                continue;
            }

            target.ExecuteClientCommand($"play {item["uniqueid"]}");
        }

        return true;
    }
    public static bool Sound_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
}