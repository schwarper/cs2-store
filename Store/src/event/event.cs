using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Event
{
    public static void Load()
    {
        Instance.RegisterListener<OnMapStart>((mapname) =>
        {
            Instance.GlobalStoreItemTypes.ForEach((type) =>
            {
                type.MapStart();
            });
        });

        Instance.RegisterListener<OnTick>(() =>
        {
            Instance.GlobalTickrate++;

            if (Instance.GlobalTickrate % 10 != 0)
            {
                return;
            }

            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (!player.Valid() || !player.PawnIsAlive)
                {
                    continue;
                }

                OnTick_CreateTrail(player);
                OnTick_ColoredSkin(player);
                OnTick_GrenadeTrail();
            }
        });

        Instance.RegisterListener<OnEntityCreated>((entity) =>
        {
            OnEntityCreated_Smoke(entity);
            OnEntityCreated_GrenadeTrail(entity);
        });

        Instance.RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            CCSPlayerController? player = @event.Userid;

            if (!player.Valid())
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
        });

        Instance.RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            CCSPlayerController? player = @event.Userid;

            if (player == null || !player.Valid()) {
                return HookResult.Continue;
            }

            if (Instance.GlobalDictionaryPlayer.TryGetValue(player, out Player? value)) {
                return HookResult.Continue;
            }

            if (value != null) {
                value.CreditIntervalTimer?.Kill();
            }
            string playername = player.PlayerName;

            Database.SavePlayer(player, playername);

            return HookResult.Continue;
        });

        Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            if (GameRules.IgnoreWarmUp())
            {
                return HookResult.Continue;
            }

            CCSPlayerController victim = @event.Userid;
            CCSPlayerController attacker = @event.Attacker;

            if (!victim.Valid() || !attacker.Valid() || victim == attacker)
            {
                return HookResult.Continue;
            }

            string playername = victim.PlayerName;

            Database.SavePlayer(victim, playername);

            if (Instance.Config.Credits["amount_kill"] > 0)
            {
                Credits.Give(attacker, Instance.Config.Credits["amount_kill"]);

                attacker.PrintToChat(Instance.Localizer["Prefix"] + Instance.Localizer["credits_earned<kill>", Instance.Config.Credits["amount_kill"]]);
            }

            return HookResult.Continue;
        });
    }
}