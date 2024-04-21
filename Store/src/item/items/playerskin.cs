using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using static CounterStrikeSharp.API.Core.Listeners;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    
    public static void Playerskin_OnPluginStart()
    {

        Item.RegisterType("playerskin", Playerskin_OnMapStart, Playerskin_OnEquip, Playerskin_OnUnequip, true, null);

        Instance.RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
        {
            CCSPlayerController player = @event.Userid;

            if (player == null || !player.IsValid)
            {
                return HookResult.Continue;
            }

            if (player.TeamNum < 2)
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
                Dictionary<string, string>? itemdata = Item.GetItem(item.Type, item.UniqueId);

                if (itemdata == null)
                {
                    return HookResult.Continue;
                }

                Instance.SetPlayerModel(player, item.UniqueId, itemdata["disable_leg"]);
            }

            return HookResult.Continue;
        });
    }
    public static void Playerskin_OnMapStart()
    {
        Instance.RegisterListener<OnServerPrecacheResources>((manifest) =>
        {
            List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("playerskin");

            foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
            {
                manifest.AddResource(item.Value["uniqueid"]);

                if (item.Value.TryGetValue("armModel", out string? armModel) && !string.IsNullOrEmpty(armModel))
                {
                    manifest.AddResource(armModel);
                }
            }
        });
    }

    public static bool Playerskin_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!item.TryGetValue("slot", out string? slot) || string.IsNullOrEmpty(slot))
        {
            return false;
        }

        if (player.TeamNum == int.Parse(item["slot"]) && player.PawnIsAlive)
        {
            Instance.SetPlayerModel(player, item["uniqueid"], item["disable_leg"]);
        }


        return true;
    }
    public static bool Playerskin_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!item.TryGetValue("slot", out string? slot) || string.IsNullOrEmpty(slot))
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

            Instance.SetPlayerModel(player, model, Instance.Config.Settings["default_model_disable_leg"]);
        }
    }

    private void SetPlayerModel(CCSPlayerController player, string model, string disable_leg)
    {
        float apply_playerskin_delay = 0.0f;

        if (Instance.Config.Settings.TryGetValue("apply_playerskin_delay", out string? value) && float.TryParse(value, CultureInfo.InvariantCulture, out float delay))
        {
            apply_playerskin_delay = delay;
        }


        if (apply_playerskin_delay > 0.0)
        {
            AddTimer(apply_playerskin_delay, () =>
            {
                player.PlayerPawn.Value?.ChangeModel(model, disable_leg);
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }
        else
        {
            player.PlayerPawn.Value?.ChangeModel(model, disable_leg);
        }
    }
}
