using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;
using Player = StoreApi.Store.Player;

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
    }

    public static void Load()
    {
        Instance.RegisterListener<OnMapStart>(OnMapStart);
        Instance.RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
        Instance.RegisterListener<OnTick>(OnTick);
        Instance.RegisterListener<OnEntityCreated>(OnEntityCreated);


        Instance.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        Instance.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        Instance.RegisterListener<OnClientAuthorized>(OnClientAuthorized);

        Instance.AddTimer(5.0f, () =>
        {
            StartCreditsTimer();
        });
    }

    public static void StartCreditsTimer()
    {
        Instance.AddTimer(Config.Credits.IntervalActiveInActive, () =>
        {
            if (GameRules.IgnoreWarmUp())
            {
                return;
            }

            List<CCSPlayerController> players = Utilities.GetPlayers();

            foreach (CCSPlayerController player in players)
            {
                if (player.IsBot)
                {
                    continue;
                }

                CsTeam team = player.Team;

                switch (team)
                {
                    case CsTeam.Terrorist:
                    case CsTeam.CounterTerrorist:
                        if (Config.Credits.AmountActive > 0)
                        {
                            Credits.Give(player, Config.Credits.AmountActive);
                            player.PrintToChatMessage("credits_earned<active>", Config.Credits.AmountActive);
                        }
                        break;

                    case CsTeam.Spectator:
                        if (Config.Credits.AmountInActive > 0)
                        {
                            Credits.Give(player, Config.Credits.AmountInActive);
                            player.PrintToChatMessage("credits_earned<inactive>", Config.Credits.AmountInActive);
                        }
                        break;
                }
            }
        }, TimerFlags.REPEAT);
    }

    public static void OnMapStart(string mapname)
    {
        Instance.GlobalStoreItemTypes.ForEach((type) =>
        {
            type.MapStart();
        });

        Database.ExecuteAsync("DELETE FROM store_items WHERE DateOfExpiration < NOW() AND DateOfExpiration > '0001-01-01 00:00:00';", null);

        List<Store_Item> itemsToRemove = Instance.GlobalStorePlayerItems
        .Where(item => item.DateOfExpiration < DateTime.Now && item.DateOfExpiration > DateTime.MinValue)
        .ToList();

        string store_equipmentTableName = Config.DatabaseConnection.DatabaseEquipTableName;

        foreach (Store_Item? item in itemsToRemove)
        {
            Database.ExecuteAsync($"DELETE FROM {store_equipmentTableName} WHERE SteamID == @SteamID AND UniqueId == @UniqueId", new { item.SteamID, item.UniqueId });

            Instance.GlobalStorePlayerItems.Remove(item);
            Instance.GlobalStorePlayerEquipments.RemoveAll(i => i.UniqueId == item.UniqueId);
        }
    }

    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        foreach (string? model in Config.DefaultModels.CounterTerrorist.Concat(Config.DefaultModels.Terrorist))
        {
            manifest.AddResource(model);
        }

        Instance.GlobalStoreItemTypes.ForEach((type) =>
        {
            type.ServerPrecacheResources(manifest);
        });
    }

    public static void OnTick()
    {
        Menu.OnTick();

        List<CCSPlayerController> players = Utilities.GetPlayers().Where(p => p.PawnIsAlive).ToList();

        foreach (CCSPlayerController? player in players)
        {
            Item_Bunnyhop.OnTick(player);
        }

        Instance.GlobalTickrate++;

        if (Instance.GlobalTickrate % 10 != 0)
        {
            return;
        }

        Instance.GlobalTickrate = 0;

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            Item_Trail.OnTick(player);
            Item_ColoredSkin.OnTick(player);
            Item_GrenadeTrail.OnTick();
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

        if (player == null)
        {
            return;
        }

        Database.LoadPlayer(player);
    }

    public static HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null)
        {
            return HookResult.Continue;
        }

        if (!Instance.GlobalDictionaryPlayer.TryGetValue(player, value: out _))
        {
            Player value = new();
            Instance.GlobalDictionaryPlayer.Add(player, value);
        }

        Instance.GlobalGiftTimeout.Add(player, 0);

        Database.UpdateVip(player);

        return HookResult.Continue;
    }

    public static HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null)
        {
            return HookResult.Continue;
        }

        if (!Instance.GlobalDictionaryPlayer.TryGetValue(player, out Player? value))
        {
            return HookResult.Continue;
        }

        value?.CreditIntervalTimer?.Kill();

        Database.SavePlayer(player);

        Instance.GlobalStorePlayers.RemoveAll(p => p.SteamID == player.SteamID);
        Instance.GlobalStorePlayerItems.RemoveAll(i => i.SteamID == player.SteamID);
        Instance.GlobalStorePlayerEquipments.RemoveAll(e => e.SteamID == player.SteamID);
        Instance.GlobalGiftTimeout.Remove(player);

        return HookResult.Continue;
    }
    public static HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (GameRules.IgnoreWarmUp())
        {
            return HookResult.Continue;
        }

        CCSPlayerController? victim = @event.Userid;
        CCSPlayerController? attacker = @event.Attacker;

        if (victim == null || attacker == null || victim == attacker)
        {
            return HookResult.Continue;
        }

        Server.NextFrame(() =>
        {
            Database.SavePlayer(victim);
        });

        if (Config.Credits.AmountKill > 0)
        {
            Credits.Give(attacker, Config.Credits.AmountKill);

            attacker.PrintToChat(Config.Tag + Instance.Localizer["credits_earned<kill>", Config.Credits.AmountKill]);
        }

        return HookResult.Continue;
    }
}
