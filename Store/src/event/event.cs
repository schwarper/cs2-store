using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
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

        foreach (Store_Item? item in itemsToRemove)
        {
            Database.ExecuteAsync("DELETE FROM store_equipment WHERE SteamID == @SteamID AND UniqueId == @UniqueId", new { item.SteamID, item.UniqueId });

            Instance.GlobalStorePlayerItems.Remove(item);
            Instance.GlobalStorePlayerEquipments.RemoveAll(i => i.UniqueId == item.UniqueId);
        }
    }

    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        foreach (string[] models in Instance.Config.DefaultModels.Values)
        {
            foreach (string model in models)
            {
                manifest.AddResource(model);
            }
        }

        Instance.GlobalStoreItemTypes.ForEach((type) =>
        {
            type.ServerPrecacheResources(manifest);
        });
    }

    public static void OnTick()
    {
        Instance.GlobalTickrate++;

        if (Instance.GlobalTickrate % 10 != 0)
        {
            return;
        }

        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            if (player == null || !player.Valid() || !player.PawnIsAlive)
            {
                continue;
            }

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

    public static HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null || !player.Valid())
        {
            return HookResult.Continue;
        }

        Task.Run(() => Database.LoadPlayer(player));

        if (!Instance.GlobalDictionaryPlayer.TryGetValue(player, out Player? value))
        {
            value = new Player();
            Instance.GlobalDictionaryPlayer.Add(player, value);
        }

        value.CreditIntervalTimer = Instance.AddTimer(Instance.Config.Credits["interval_active_inactive"], () =>
        {
            if (GameRules.IgnoreWarmUp())
            {
                return;
            }

            CsTeam Team = player.Team;

            switch (Team)
            {
                case CsTeam.Terrorist:
                case CsTeam.CounterTerrorist:
                    {
                        if (Instance.Config.Credits["amount_active"] > 0)
                        {
                            Credits.Give(player, Instance.Config.Credits["amount_active"]);

                            player.PrintToChatMessage("credits_earned<active>", Instance.Config.Credits["amount_active"]);
                        }

                        break;
                    }
                case CsTeam.Spectator:
                    {
                        if (Instance.Config.Credits["amount_inactive"] > 0)
                        {
                            Credits.Give(player, Instance.Config.Credits["amount_inactive"]);

                            player.PrintToChatMessage("credits_earned<inactive>", Instance.Config.Credits["amount_inactive"]);
                        }

                        break;
                    }
            }
        }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);

        return HookResult.Continue;
    }

    public static HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null || !player.Valid())
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

        return HookResult.Continue;
    }

    public static HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (GameRules.IgnoreWarmUp())
        {
            return HookResult.Continue;
        }

        CCSPlayerController victim = @event.Userid;
        CCSPlayerController attacker = @event.Attacker;

        if (victim == null || attacker == null || !victim.Valid() || !attacker.Valid() || victim == attacker)
        {
            return HookResult.Continue;
        }

        Server.NextFrame(() =>
        {
            Database.SavePlayer(victim);
        });

        if (Instance.Config.Credits["amount_kill"] > 0)
        {
            Credits.Give(attacker, Instance.Config.Credits["amount_kill"]);

            attacker.PrintToChat(Instance.Config.Tag + Instance.Localizer["credits_earned<kill>", Instance.Config.Credits["amount_kill"]]);
        }

        return HookResult.Continue;
    }
}
