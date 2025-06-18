using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using static Store.ConfigConfig;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

[StoreItemType("playerskin")]
public class ItemPlayerSkin : IItemModule
{
    private static bool ForceModelDefault { get; set; }
    private static bool DisableLeg(Dictionary<string, string> item)
    {
        return item.ContainsKey("disable_leg") && item["disable_leg"] is "true" or "1";
    }

    public bool Equipable => true;
    public bool? RequiresAlive => null;

    public void OnPluginStart()
    {
        if (!Item.IsAnyItemExistInType("playerskin")) return;
        
        foreach (string command in Config.Commands.PlayerSkinsOff)
            Instance.AddCommand(command, "Turn off playerskins models", Command_Model0);

        foreach (string command in Config.Commands.PlayerSkinsOn)
            Instance.AddCommand(command, "Turn on playerskins models", Command_Model1);

        Instance.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        Instance.RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Pre);
    }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        foreach (var item in Item.GetItemsByType("playerskin"))
        {
            manifest.AddResource(item.Value["model"]);
            if (item.Value.TryGetValue("armModel", out string? armModel) && !string.IsNullOrEmpty(armModel))
                manifest.AddResource(armModel);
        }
    }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!item.TryGetValue("slot", out string? slot) || string.IsNullOrEmpty(slot) || ForceModelDefault)
            return false;

        player.ChangeModelDelay(item["model"], DisableLeg(item), int.Parse(item["slot"]), item.GetValueOrDefault("skin"));
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        if (!update || !item.TryGetValue("slot", out string? slot) || string.IsNullOrEmpty(slot) || ForceModelDefault)
            return false;

        var defaultModel = GetDefaultModel(player);
        if (defaultModel.HasValue)
            player.ChangeModelDelay(defaultModel.Value.modelname, defaultModel.Value.disableleg, player.TeamNum, defaultModel.Value.skin);

        return true;
    }

    private static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null || player.TeamNum < 2)
            return HookResult.Continue;

        var modelData = GetModel(player, player.TeamNum);
        if (modelData.HasValue)
            player.ChangeModelDelay(modelData.Value.modelname, modelData.Value.disableleg, player.TeamNum, modelData.Value.skin);

        return HookResult.Continue;
    }

    private static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Instance.InspectList.Clear();
        ForceModelDefault = false;
        return HookResult.Continue;
    }

    private static void Command_Model0(CCSPlayerController? player, CommandInfo info)
    {
        if (string.IsNullOrEmpty(Config.Permissions.Model0Model1Flag) || !AdminManager.PlayerHasPermissions(player, Config.Permissions.Model0Model1Flag))
            return;

        var players = Utilities.GetPlayers();
        foreach (CCSPlayerController target in players)
        {
            var modelDatas = GetDefaultModel(target);
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

        var players = Utilities.GetPlayers();
        foreach (CCSPlayerController target in players)
        {
            var modelDatas = GetModel(target, target.TeamNum);
            if (modelDatas.HasValue)
                target.PlayerPawn.Value?.ChangeModel(modelDatas.Value.modelname, modelDatas.Value.disableleg, modelDatas.Value.skin);
        }

        Server.PrintToChatAll(Config.Settings.Tag + Instance.Localizer["css_model1", player?.PlayerName ?? Instance.Localizer["Console"]]);
        ForceModelDefault = false;
    }

    private static (string modelname, bool disableleg, string? skin)? GetModel(CCSPlayerController player, int teamnum)
    {
        StoreEquipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamId == player.SteamID && p.Type == "playerskin" && (p.Slot == teamnum || p.Slot == 1));
        return item == null || ForceModelDefault ? GetDefaultModel(player) : GetStoreModel(item);
    }

    private static (string modelname, bool disableleg, string? skin)? GetDefaultModel(CCSPlayerController player)
    {
        var modelsArray = player.Team == CsTeam.CounterTerrorist ? Config.DefaultModels.CounterTerrorist : Config.DefaultModels.Terrorist;
        return modelsArray.Count > 0
            ? (modelsArray[Instance.Random.Next(0, modelsArray.Count)], Config.DefaultModels.DefaultModelDisableLeg, null)
            : null;
    }

    private static (string modelname, bool disableleg, string? skin)? GetStoreModel(StoreEquipment item)
    {
        var itemdata = Item.GetItem(item.UniqueId);
        return itemdata == null ? null : (itemdata["model"], DisableLeg(itemdata), itemdata.GetValueOrDefault("skin"));
    }

    public static void Inspect(CCSPlayerController player, string model, string? skin)
    {
        CBaseModelEntity? entity = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        if (entity == null || !entity.IsValid || player.PlayerPawn.Value is not { } playerPawn)
            return;

        Vector origin = GetFrontPosition(playerPawn.AbsOrigin!, playerPawn.EyeAngles);
        QAngle modelAngles = new(0, playerPawn.EyeAngles.Y + 180, 0);

        entity.Spawnflags = 256u;
        entity.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        entity.Teleport(origin, modelAngles, playerPawn.AbsVelocity);
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
        Instance.AddTimer(1.0f, () => RotateEntity(entity, 0.0f));
    }

    private static void RotateEntity(CBaseModelEntity entity, float elapsed)
    {
        if (!entity.IsValid)
            return;

        const float totalTime = 4.0f;
        const float totalRotation = 360.0f;
        const float interval = 0.04f;
        const float rotationStep = (interval / totalTime) * totalRotation;

        QAngle currentAngles = entity.AbsRotation!;
        entity.Teleport(null, new QAngle(currentAngles.X, currentAngles.Y + rotationStep, currentAngles.Z));

        if (elapsed < totalTime)
            Instance.AddTimer(interval, () => RotateEntity(entity, elapsed + interval));
        else
        {
            Instance.InspectList.Remove(entity);
            entity.Remove();
        }
    }

    private static Vector GetFrontPosition(Vector position, QAngle angles, float distance = 100.0f)
    {
        float radYaw = angles.Y * (MathF.PI / 180.0f);
        return position + new Vector(MathF.Cos(radYaw), MathF.Sin(radYaw), 0) * distance;
    }
}
