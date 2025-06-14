using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using static CounterStrikeSharp.API.Core.Listeners;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Event
{
    public static void Unload()
    {
        Instance.RemoveListener<OnMapStart>(OnMapStart);
        Instance.RemoveListener<OnServerPrecacheResources>(OnServerPrecacheResources);
        Instance.RemoveListener<OnTick>(OnTick);
        Instance.RemoveListener<OnEntityCreated>(OnEntityCreated);
        Instance.RemoveListener<OnClientAuthorized>(OnClientAuthorized);
        Instance.RemoveListener<CheckTransmit>(OnCheckTransmit);
    }

    public static void Load()
    {
        Instance.RegisterListener<OnMapStart>(OnMapStart);
        Instance.RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
        Instance.RegisterListener<OnTick>(OnTick);
        Instance.RegisterListener<OnEntityCreated>(OnEntityCreated);
        Instance.RegisterListener<OnClientAuthorized>(OnClientAuthorized);
        Instance.RegisterListener<CheckTransmit>(OnCheckTransmit);

        Instance.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        Instance.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        Instance.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        Instance.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);

        Instance.AddTimer(5.0f, StartCreditsTimer);
    }

    private static void StartCreditsTimer()
    {
        if (!Config.Credits.TryGetValue("default", out Config_Credits? defaultCredits) ||
            defaultCredits.IntervalActiveInActive <= 0)
            return;

        List<KeyValuePair<string, Config_Credits>> orderedCredits = Config.Credits
            .Where(x => x.Key != "default" && (x.Value.AmountActive > 0 || x.Value.AmountInActive > 0))
            .ToList();

        Instance.AddTimer(defaultCredits.IntervalActiveInActive, () =>
        {
            if (GameRules.IgnoreWarmUp())
                return;

            List<CCSPlayerController> players = Utilities.GetPlayers()
                .Where(p => !p.IsBot)
                .ToList();

            foreach (CCSPlayerController player in players)
            {
                bool granted = orderedCredits.Where(credits => AdminManager.PlayerHasPermissions(player, credits.Key)).Any(credits => GiveCreditsTimer(player, credits.Value.AmountActive, credits.Value.AmountInActive));

                if (!granted)
                {
                    GiveCreditsTimer(player, defaultCredits.AmountActive, defaultCredits.AmountInActive);
                }
            }

        }, TimerFlags.REPEAT);
    }

    private static bool GiveCreditsTimer(CCSPlayerController player, int active, int inactive)
    {
        switch (player.Team)
        {
            case CsTeam.Terrorist:
            case CsTeam.CounterTerrorist when active > 0:
                Credits.Give(player, active);
                player.PrintToChatMessage("credits_earned<active>", active);
                return true;
            case CsTeam.Spectator when inactive > 0:
                Credits.Give(player, inactive);
                player.PrintToChatMessage("credits_earned<inactive>", inactive);
                return true;
            case CsTeam.None:
            default: return false;
        }
    }


    public static void OnMapStart(string mapname)
    {
        foreach (var module in ItemModuleManager.Modules)
        {
            module.Value.OnMapStart();
        }
    }

    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        foreach (string? model in Config.DefaultModels.CounterTerrorist.Concat(Config.DefaultModels.Terrorist))
        {
            manifest.AddResource(model);
        }

        foreach (var module in ItemModuleManager.Modules)
        {
            module.Value.OnServerPrecacheResources(manifest);
        }
    }

    public static void OnTick()
    {
        List<CCSPlayerController> players = Utilities.GetPlayers();

        foreach (CCSPlayerController player in players)
        {
            if (!player.PawnIsAlive) continue;

            Item_Bunnyhop.OnTick(player);
        }

        Instance.GlobalTickrate++;

        if (Instance.GlobalTickrate % 10 != 0) return;

        Instance.GlobalTickrate = 0;

        foreach (CCSPlayerController player in players)
        {
            Item_Trail.OnTick(player);
            Item_ColoredSkin.OnTick(player);
        }
    }

    public static void OnEntityCreated(CEntityInstance entity)
    {
        Item_Smoke.OnEntityCreated(entity);
        Item_GrenadeTrail.OnEntityCreated(entity);
        Item_CustomWeapon.OnEntityCreated(entity);
    }

    private static void OnClientAuthorized(int playerSlot, SteamID steamId)
    {
        CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);

        if (player == null) return;

        Database.LoadPlayer(player);
    }

    public static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Item.RemoveExpiredItems();
        return HookResult.Continue;
    }

    public static HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null) return HookResult.Continue;

        if (!Instance.GlobalDictionaryPlayer.ContainsKey(player))
        {
            Instance.GlobalDictionaryPlayer[player] = new PlayerTimer();
        }

        Instance.GlobalGiftTimeout[player] = 0;

        Database.UpdateVip(player);

        return HookResult.Continue;
    }

    public static HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null) return HookResult.Continue;

        Item_Trail.HideTrailPlayerList.Remove(player);

        if (Instance.GlobalDictionaryPlayer.TryGetValue(player, out PlayerTimer? value))
        {
            value.CreditIntervalTimer?.Kill();
        }

        Database.SavePlayer(player);

        Instance.GlobalStorePlayers.RemoveAll(p => p.SteamID == player.SteamID);
        Instance.GlobalStorePlayerItems.RemoveAll(i => i.SteamID == player.SteamID);
        Instance.GlobalStorePlayerEquipments.RemoveAll(e => e.SteamID == player.SteamID);
        Instance.GlobalGiftTimeout.Remove(player);

        return HookResult.Continue;
    }

    public static HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (GameRules.IgnoreWarmUp()) return HookResult.Continue;

        CCSPlayerController? victim = @event.Userid;
        CCSPlayerController? attacker = @event.Attacker;

        if (victim == null || attacker == null || victim == attacker) return HookResult.Continue;

        Server.NextFrame(() => Database.SavePlayer(victim));

        int amountKill = 0;

        KeyValuePair<string, Config_Credits> credits = Config.Credits
            .Where(x => x.Key != "default" && x.Value.AmountKill > 0)
            .FirstOrDefault(x => AdminManager.PlayerHasPermissions(attacker, x.Key));

        if (credits.Value is not null)
        {
            amountKill = credits.Value.AmountKill;
        }

        if (amountKill <= 0 && Config.Credits.TryGetValue("default", out Config_Credits? defaultCredits))
        {
            amountKill = defaultCredits.AmountKill;
        }

        if (amountKill > 0)
        {
            Credits.Give(attacker, amountKill);
            attacker.PrintToChat($"{Config.Settings.Tag}{Instance.Localizer["credits_earned<kill>", amountKill]}");
        }

        return HookResult.Continue;
    }

    public static HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null) return HookResult.Continue;

        List<Store_Equipment> currentItems = Instance.GlobalStorePlayerEquipments.FindAll(p => p.SteamID == player.SteamID);

        foreach (Store_Equipment currentItem in currentItems)
        {
            Dictionary<string, string>? item = Item.GetItem(currentItem.UniqueId);

            if (item == null) continue;

            if (item.TryGetValue("team", out string? teamStr) && int.TryParse(teamStr, out int team) && team >= 1 && team <= 3 && @event.Team != team)
            {
                Item.Unequip(player, item, true);
            }
        }

        return HookResult.Continue;
    }

    public static void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (Instance.InspectList.Count == 0 && Item_Trail.TrailList.Count == 0)
            return;

        foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
        {
            if (player is not { IsValid: true, IsBot: false })
                continue;

            ulong playerSteamId = player.SteamID;

            foreach ((CBaseModelEntity? entity, CCSPlayerController? owner) in Instance.InspectList)
            {
                if (owner.IsValid && owner.SteamID != playerSteamId)
                    info.TransmitEntities.Remove(entity);
            }

            if (!Item_Trail.HideTrailPlayerList.Contains(player))
                continue;

            foreach ((CEntityInstance? entity, CCSPlayerController? owner) in Item_Trail.TrailList)
            {
                if (owner.IsValid && owner.SteamID != playerSteamId)
                    info.TransmitEntities.Remove(entity);
            }
        }
    }
}