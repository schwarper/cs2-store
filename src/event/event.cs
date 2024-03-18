using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static Store.Store;

namespace Store;

internal static class Event
{
    internal static void Load()
    {
        Instance.RegisterListener<OnMapStart>((mapname) =>
        {
            Instance.GlobalStoreItemTypes.ForEach((type) =>
            {
                type.MapStart();
            });
        });

        Instance.RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            CCSPlayerController? player = @event.Userid;

            if (!player.Valid())
            {
                return HookResult.Continue;
            }

            Task.Run(async () =>
            {
                await Database.LoadPlayer(player);
            });

            Instance.GlobalDictionaryPlayer.Add(player, new Player());

            Instance.GlobalDictionaryPlayer[player].CreditIntervalTimer = Instance.AddTimer(Instance.Config.Credits["interval_active_inactive"], () =>
            {
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

            if (!player.Valid())
            {
                return HookResult.Continue;
            }

            string playername = player.PlayerName;

            Task.Run(async () =>
            {
                await Database.SavePlayer(player, playername);
            });

            /* TO DO
             * Null easier if you don't add anything else
            */
            if (Instance.GlobalDictionaryPlayer.ContainsKey(player) && Instance.GlobalDictionaryPlayer[player] != null)
            {
                Instance.GlobalDictionaryPlayer[player].CreditIntervalTimer?.Kill();
            }

            return HookResult.Continue;
        });

        Instance.RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            CCSPlayerController victim = @event.Userid;
            CCSPlayerController attacker = @event.Attacker;

            if (!victim.Valid() || attacker.Valid() || victim == attacker)
            {
                return HookResult.Continue;
            }

            string playername = victim.PlayerName;

            Task.Run(async () =>
            {
                await Database.SavePlayer(victim, playername);
            });

            if (Instance.Config.Credits["amount_kill"] > 0)
            {
                Credits.Give(attacker, Instance.Config.Credits["amount_kill"]);

                attacker.PrintToChat(Instance.Localizer["Prefix"] + Instance.Localizer["credits_earned<kill>", Instance.Config.Credits["amount_kill"]]);
            }

            return HookResult.Continue;
        });
    }
}