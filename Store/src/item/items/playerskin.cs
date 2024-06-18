using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_PlayerSkin
{
    public static bool ForceModelDefault { get; set; } = false;

    public static void OnPluginStart()
    {
        Instance.AddCommand("css_model0", "Model0", Command_Model0);
        Instance.AddCommand("css_model1", "Model1", Command_Model1);

        Item.RegisterType("playerskin", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        Instance.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
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
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!item.TryGetValue("slot", out string? slot) || string.IsNullOrEmpty(slot))
        {
            return false;
        }

        if (ForceModelDefault)
        {
            return false;
        }

        SetPlayerModel(player, item["uniqueid"], item["disable_leg"], int.Parse(item["slot"]));

        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!item.TryGetValue("slot", out string? slot) || string.IsNullOrEmpty(slot))
        {
            return false;
        }

        if (ForceModelDefault)
        {
            return false;
        }

        if (player.TeamNum == int.Parse(item["slot"]))
        {
            SetDefaultModel(player);
        }

        return true;
    }
    public static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null)
        {
            return HookResult.Continue;
        }

        if (player.TeamNum < 2)
        {
            return HookResult.Continue;
        }


        Store_Equipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "playerskin" && p.Slot == player.TeamNum);

        if (item == null || ForceModelDefault)
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

            SetPlayerModel(player, item.UniqueId, itemdata["disable_leg"], item.Slot);
        }

        return HookResult.Continue;
    }
    public static void SetDefaultModel(CCSPlayerController player)
    {
        string[] modelsArray = player.Team == CsTeam.CounterTerrorist ? Instance.Config.DefaultModels["ct"] : Instance.Config.DefaultModels["t"];
        int maxIndex = modelsArray.Length;

        if (maxIndex > 0)
        {
            int randomnumber = Instance.Random.Next(0, maxIndex - 1);

            string model = modelsArray[randomnumber];

            SetPlayerModel(player, model, Instance.Config.Settings["default_model_disable_leg"], player.TeamNum);
        }
    }
    private static void SetPlayerModel(CCSPlayerController player, string model, string disable_leg, int slotNumber)
    {
        float apply_delay = 0.1f;

        if (Instance.Config.Settings.TryGetValue("apply_delay", out string? value) && float.TryParse(value, CultureInfo.InvariantCulture, out float delay))
        {
            apply_delay = float.MaxNumber(0.1f, delay);
        }

        Instance.AddTimer(apply_delay, () =>
        {
            if (player.IsValid && player.TeamNum == slotNumber && player.PawnIsAlive)
            {
                player.PlayerPawn.Value?.ChangeModel(model, disable_leg);
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    private static void Command_Model0(CCSPlayerController? player, CommandInfo info)
    {
        if (Instance.Config.Settings.TryGetValue("model0_model1_flag", out string? value) && !string.IsNullOrEmpty(value))
        {
            if (!AdminManager.PlayerHasPermissions(player, value))
            {
                return;
            }
        }
        else
        {
            return;
        }

        foreach (var target in Utilities.GetPlayers())
        {
            SetDefaultModel(target);
        }

        Server.PrintToChatAll(Instance.Config.Tag + Instance.Localizer["css_model0", player?.PlayerName ?? Instance.Localizer["Console"]]);

        ForceModelDefault = true;
    }

    private static void Command_Model1(CCSPlayerController? player, CommandInfo info)
    {
        if (Instance.Config.Settings.TryGetValue("model0_model1_flag", out string? value) && !string.IsNullOrEmpty(value))
        {
            if (!AdminManager.PlayerHasPermissions(player, value))
            {
                return;
            }
        }
        else
        {
            return;
        }

        foreach (var target in Utilities.GetPlayers())
        {
            Store_Equipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == target.SteamID && p.Type == "playerskin" && p.Slot == target.TeamNum);

            if (item != null)
            {
                Dictionary<string, string>? itemdata = Item.GetItem(item.Type, item.UniqueId);

                if (itemdata == null)
                {
                    continue;
                }

                SetPlayerModel(target, item.UniqueId, itemdata["disable_leg"], item.Slot);
            }
        }

        Server.PrintToChatAll(Instance.Config.Tag + Instance.Localizer["css_model0", player?.PlayerName ?? Instance.Localizer["Console"]]);

        ForceModelDefault = false;
    }
}