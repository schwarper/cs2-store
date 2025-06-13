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
    private static bool DisableLeg(Dictionary<string, string> item)
    {
        return item.ContainsKey("disable_leg") && item["disable_leg"] is "true" or "1";
    }

    public static void OnPluginStart()
    {
        Item.RegisterType("playerskin", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.IsAnyItemExistInType("playerskin"))
        {
            foreach (string command in Config.Commands.ModelOff)
                Instance.AddCommand(command, "Turn off playerskins models", Command_Model0);

            foreach (string command in Config.Commands.ModelOn)
                Instance.AddCommand(command, "Turn on playerskins models", Command_Model1);

            Instance.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            Instance.RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Pre);
        }
    }

    public static void OnMapStart() { }

    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        foreach (KeyValuePair<string, Dictionary<string, string>> item in Item.GetItemsByType("playerskin"))
        {
            manifest.AddResource(item.Value["model"]);
            if (item.Value.TryGetValue("armModel", out string? armModel) && !string.IsNullOrEmpty(armModel))
                manifest.AddResource(armModel);
        }
    }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!item.TryGetValue("slot", out string? slot) || string.IsNullOrEmpty(slot) || ForceModelDefault)
            return false;

        player.ChangeModelDelay(item["model"], DisableLeg(item), int.Parse(item["slot"]), item.GetValueOrDefault("skin"));
        return true;
    }

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        if (!update || !item.TryGetValue("slot", out string? slot) || string.IsNullOrEmpty(slot) || ForceModelDefault)
            return false;

        (string modelname, bool disableleg, string? skin)? defaultModel = GetDefaultModel(player);
        if (defaultModel.HasValue)
            player.ChangeModelDelay(defaultModel.Value.modelname, defaultModel.Value.disableleg, player.TeamNum, defaultModel.Value.skin);

        return true;
    }

    public static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null || player.TeamNum < 2)
            return HookResult.Continue;

        (string modelname, bool disableleg, string? skin)? modelData = GetModel(player, player.TeamNum);
        if (modelData.HasValue)
            player.ChangeModelDelay(modelData.Value.modelname, modelData.Value.disableleg, player.TeamNum, modelData.Value.skin);

        return HookResult.Continue;
    }
    
    public static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Instance.InspectList.Clear();
        ForceModelDefault = false;
        return HookResult.Continue;
    }

    private static void Command_Model0(CCSPlayerController? player, CommandInfo info)
    {
        if (string.IsNullOrEmpty(Config.Permissions.Model0Model1Flag) || !AdminManager.PlayerHasPermissions(player, Config.Permissions.Model0Model1Flag))
            return;

        List<CCSPlayerController> players = Utilities.GetPlayers();
        foreach (CCSPlayerController target in players)
        {
            (string modelname, bool disableleg, string? skin)? modelDatas = GetDefaultModel(target);
            if (modelDatas.HasValue)
                target.PlayerPawn.Value?.ChangeModel(modelDatas.Value.modelname, modelDatas.Value.disableleg, modelDatas.Value.skin);
        }

        Server.PrintToChatAll(Config.Settings.Tag + Instance.Localizer["css_model0", player?.PlayerName ?? Instance.Localizer["Console"]]);
        ForceModelDefault = true;
    }

    private static void Command_Model1(CCSPlayerController? player, CommandInfo info)
    {
        if (string.IsNullOrEmpty(Config.Permissions.Model0Model1Flag) || !AdminManager.PlayerHasPermissions(player, Config.Permissions.Model0Model1Flag))
            return;

        List<CCSPlayerController> players = Utilities.GetPlayers();
        foreach (CCSPlayerController target in players)
        {
            (string modelname, bool disableleg, string? skin)? modelDatas = GetModel(target, target.TeamNum);
            if (modelDatas.HasValue)
                target.PlayerPawn.Value?.ChangeModel(modelDatas.Value.modelname, modelDatas.Value.disableleg, modelDatas.Value.skin);
        }

        Server.PrintToChatAll(Config.Settings.Tag + Instance.Localizer["css_model1", player?.PlayerName ?? Instance.Localizer["Console"]]);
        ForceModelDefault = false;
    }

    private static (string modelname, bool disableleg, string? skin)? GetModel(CCSPlayerController player, int teamnum)
    {
        Store_Equipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "playerskin" && (p.Slot == teamnum || p.Slot == 1));
        return item == null || ForceModelDefault ? GetDefaultModel(player) : GetStoreModel(item);
    }

    private static (string modelname, bool disableleg, string? skin)? GetDefaultModel(CCSPlayerController player)
    {
        var modelsArray = player.Team == CsTeam.CounterTerrorist ? Config.DefaultModels.CounterTerrorist : Config.DefaultModels.Terrorist;
        return modelsArray.Count > 0
            ? (modelsArray[Instance.Random.Next(0, modelsArray.Count)], Config.DefaultModels.DefaultModelDisableLeg, null)
            : null;
    }

    private static (string modelname, bool disableleg, string? skin)? GetStoreModel(Store_Equipment item)
    {
        Dictionary<string, string>? itemdata = Item.GetItem(item.UniqueId);
        return itemdata == null ? null : (itemdata["model"], DisableLeg(itemdata), itemdata.GetValueOrDefault("skin"));
    }

    public static void Inspect(CCSPlayerController player, string model, string? skin)
    {
        CBaseModelEntity? entity = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        if (entity == null || !entity.IsValid || player.PlayerPawn.Value is not CCSPlayerPawn playerPawn)
            return;

        Vector _origin = GetFrontPosition(playerPawn.AbsOrigin!, playerPawn.EyeAngles);
        QAngle modelAngles = new(0, playerPawn.EyeAngles.Y + 180, 0);

        entity.Spawnflags = 256u;
        entity.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        entity.Teleport(_origin, modelAngles, playerPawn.AbsVelocity);
        entity.DispatchSpawn();

        Server.NextFrame(() =>
        {
            if (entity.IsValid)
            {
                entity.SetModel(model);
                if (skin != null)
                    entity.AcceptInput("Skin", null, entity, skin);
            }
        });

        Instance.InspectList[entity] = player;
        Instance.AddTimer(1.0f, () => RotateEntity(player, entity, 0.0f));
    }

    public static void RotateEntity(CCSPlayerController player, CBaseModelEntity entity, float elapsed)
    {
        if (!entity.IsValid)
            return;

        float totalTime = 4.0f;
        float totalRotation = 360.0f;
        float interval = 0.04f;
        float rotationStep = (interval / totalTime) * totalRotation;

        QAngle currentAngles = entity.AbsRotation!;
        entity.Teleport(null, new QAngle(currentAngles.X, currentAngles.Y + rotationStep, currentAngles.Z), null);

        if (elapsed < totalTime)
            Instance.AddTimer(interval, () => RotateEntity(player, entity, elapsed + interval));
        else
        {
            Instance.InspectList.Remove(entity);
            entity.Remove();
        }
    }

    public static Vector GetFrontPosition(Vector position, QAngle angles, float distance = 100.0f)
    {
        float radYaw = angles.Y * (MathF.PI / 180.0f);
        return position + new Vector(MathF.Cos(radYaw), MathF.Sin(radYaw), 0) * distance;
    }
}
