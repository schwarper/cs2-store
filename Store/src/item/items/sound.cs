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
        Instance.RegisterListener<OnServerPrecacheResources>((manifest) =>
        {
            List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("trail");

            foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
            {
                if (item.Value["uniqueid"].Contains(".vsnd"))
                {
                    manifest.AddResource(item.Value["uniqueid"]);
                }
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