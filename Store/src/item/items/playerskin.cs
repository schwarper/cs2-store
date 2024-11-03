using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_PlayerSkin
{
    public static bool ForceModelDefault { get; set; } = false;

    public static void OnPluginStart()
    {
        Item.RegisterType("playerskin", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.GetItemsByType("playerskin").Count > 0)
        {
            Instance.AddCommand("css_model0", "Model0", Command_Model0);
            Instance.AddCommand("css_model1", "Model1", Command_Model1);

            Instance.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Instance.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
            Instance.RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Pre);
        }
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

        item.TryGetValue("skin", out string? skn);

        player.ChangeModelDelay(item["uniqueid"], item["disable_leg"] is "true" or "1", int.Parse(item["slot"]), skn);

        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        if (!update)
        {
            return true;
        }

        if (!item.TryGetValue("slot", out string? slot) || string.IsNullOrEmpty(slot))
        {
            return false;
        }

        if (ForceModelDefault)
        {
            return false;
        }

        (string modelname, bool disableleg, string? skin)? defaultModel = GetDefaultModel(player);

        if (defaultModel.HasValue)
        {
            player.ChangeModelDelay(defaultModel.Value.modelname, defaultModel.Value.disableleg, player.TeamNum, defaultModel.Value.skin);
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

        (string modelname, bool disableleg, string? skin)? modelData = GetModel(player, player.TeamNum);

        if (modelData.HasValue)
        {
            player.ChangeModelDelay(modelData.Value.modelname, modelData.Value.disableleg, player.TeamNum, modelData.Value.skin);
        }

        return HookResult.Continue;
    }

    public static HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null)
        {
            return HookResult.Continue;
        }

        if (@event.Disconnect || !player.PawnIsAlive)
        {
            return HookResult.Continue;
        }

        if (@event.Team == 2 || @event.Team == 3)
        {
            (string modelname, bool disableleg, string? skin)? modelDatas = GetModel(player, @event.Team);

            if (modelDatas.HasValue)
            {
                player.ChangeModelDelay(modelDatas.Value.modelname, modelDatas.Value.disableleg, @event.Team, modelDatas.Value.skin);
            }
        }

        return HookResult.Continue;
    }

    public static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        ForceModelDefault = false;
        return HookResult.Continue;
    }

    private static void Command_Model0(CCSPlayerController? player, CommandInfo info)
    {
        string flag = Config.Settings.Model0Model1Flag;

        if (string.IsNullOrEmpty(flag) || !AdminManager.PlayerHasPermissions(player, flag))
        {
            return;
        }

        foreach (CCSPlayerController target in Utilities.GetPlayers())
        {
            (string modelname, bool disableleg, string? skin)? modelDatas = GetDefaultModel(target);

            if (modelDatas.HasValue)
            {
                target.PlayerPawn.Value!.ChangeModel(modelDatas.Value.modelname, modelDatas.Value.disableleg, modelDatas.Value.skin);
            }
        }

        Server.PrintToChatAll(Config.Tag + Instance.Localizer["css_model0", player?.PlayerName ?? Instance.Localizer["Console"]]);

        ForceModelDefault = true;
    }

    private static void Command_Model1(CCSPlayerController? player, CommandInfo info)
    {
        string flag = Config.Settings.Model0Model1Flag;

        if (string.IsNullOrEmpty(flag) || !AdminManager.PlayerHasPermissions(player, flag))
        {
            return;
        }

        foreach (CCSPlayerController target in Utilities.GetPlayers())
        {
            (string modelname, bool disableleg, string? skin)? modelDatas = GetModel(target, target.TeamNum);

            if (modelDatas.HasValue)
            {
                target.PlayerPawn.Value!.ChangeModel(modelDatas.Value.modelname, modelDatas.Value.disableleg, modelDatas.Value.skin);
            }
        }

        Server.PrintToChatAll(Config.Tag + Instance.Localizer["css_model1", player?.PlayerName ?? Instance.Localizer["Console"]]);

        ForceModelDefault = false;
    }

    private static (string modelname, bool disableleg, string? skin)? GetModel(CCSPlayerController player, int teamnum)
    {
        Store_Equipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "playerskin" && (p.Slot == teamnum || p.Slot == 1));

        if (item == null || ForceModelDefault)
        {
            return GetDefaultModel(player);
        }
        else
        {
            return GetStoreModel(item);
        }
    }

    private static (string modelname, bool disableleg, string? skin)? GetDefaultModel(CCSPlayerController player)
    {
        string[] modelsArray = player.Team == CsTeam.CounterTerrorist ? Config.DefaultModels.CounterTerrorist : Config.DefaultModels.Terrorist;
        int maxIndex = modelsArray.Length;

        if (maxIndex > 0)
        {
            int randomnumber = Instance.Random.Next(0, maxIndex - 1);

            string model = modelsArray[randomnumber];

            return (model, Config.Settings.DefaultModelDisableLeg, null);
        }

        return null;
    }

    private static (string modelname, bool disableleg, string? skin)? GetStoreModel(Store_Equipment item)
    {
        Dictionary<string, string>? itemdata = Item.GetItem(item.Type, item.UniqueId);

        if (itemdata == null)
        {
            return null;
        }

        itemdata.TryGetValue("skin", out string? skn);

        return (item.UniqueId, itemdata["disable_leg"] is "true" or "1", skn);
    }
}