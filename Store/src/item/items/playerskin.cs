using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public static void Playerskin_OnPluginStart()
    {
        new StoreAPI().RegisterType("playerskin", Playerskin_OnMapStart, Playerskin_OnEquip, Playerskin_OnUnequip, true, null);

        Instance.RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
        {
            CCSPlayerController player = @event.Userid;

            if (player == null || !player.IsValid)
            {
                return HookResult.Continue;
            }

            if (player.Team != CsTeam.Terrorist && player.Team != CsTeam.CounterTerrorist)
            {
                return HookResult.Continue;
            }

            CCSPlayerPawn? playerPawn = player.PlayerPawn?.Value;

            if (playerPawn == null)
            {
                return HookResult.Continue;
            }

            Store_Equipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "playerskin" && p.Slot == player.TeamNum);

            if (item == null)
            {
                SetDefaultModel(player);
            }
            else
            {
                playerPawn.ChangeModel(item.UniqueId);
            }

            return HookResult.Continue;
        });
    }
    public static void Playerskin_OnMapStart()
    {
        IEnumerable<string> playerSkinItems = Instance.Config.Items
        .SelectMany(wk => wk.Value)
        .Where(kvp => kvp.Value["type"] == "playerskin")
        .Select(kvp => kvp.Value["uniqueid"]);

        Instance.RegisterListener<OnServerPrecacheResources>((manifest) =>
        {
            foreach (string UniqueId in playerSkinItems)
            {
                manifest.AddResource(UniqueId);
            }
        });
    }

    public static bool Playerskin_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (item.TryGetValue("slot", out string? slot) || string.IsNullOrEmpty(slot))
        {
            return false;
        }

        if (player.TeamNum == int.Parse(item["slot"]))
        {
            player.PlayerPawn.Value?.ChangeModel(item["uniqueid"]);
        }

        return true;
    }
    public static bool Playerskin_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (item.TryGetValue("slot", out string? slot) || string.IsNullOrEmpty(slot))
        {
            return false;
        }

        if (player.TeamNum == int.Parse(item["slot"]))
        {
            SetDefaultModel(player);
        }

        return true;
    }

    public static void SetDefaultModel(CCSPlayerController player)
    {
        string[] modelsArray = player.Team == CsTeam.CounterTerrorist ? Instance.Config.DefaultModels["ct"] : Instance.Config.DefaultModels["t"];
        int maxIndex = modelsArray.Length;

        if (maxIndex > 0)
        {
            int randomnumber = Instance.random.Next(0, maxIndex - 1);

            string model = modelsArray[randomnumber];

            player.PlayerPawn.Value?.ChangeModel(model);
        }
    }
}