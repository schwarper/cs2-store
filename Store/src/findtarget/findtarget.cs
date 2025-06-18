using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Entities;
using static Store.ConfigConfig;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class FindTarget
{
    public class TargetFind
    {
        public List<CCSPlayerController> Players = [];
        public StorePlayer? StorePlayer;
        public string? TargetName;
    }

    public static TargetFind Find(CommandInfo command, bool singleTarget, bool allowSteamId)
    {
        TargetResult targetResult = command.GetArgTargetResult(1);
        if (targetResult.Players.Count == 0)
        {
            if (allowSteamId)
            {
                string arg = command.GetArg(1).Trim();

                if (!SteamID.TryParse(arg, out SteamID? steamId) || steamId == null)
                {
                    if (ulong.TryParse(arg, out ulong steamIdNum))
                    {
                        steamId = new SteamID(steamIdNum);
                    }
                }

                if (steamId != null)
                {
                    StorePlayer? playerdata = Instance.GlobalStorePlayers
                        .SingleOrDefault(player => player.SteamId == steamId.SteamId64);

                    if (playerdata == null)
                    {
                        playerdata = new StorePlayer
                        {
                            SteamId = steamId.SteamId64,
                            Credits = 0,
                            PlayerName = steamId.SteamId64.ToString()
                        };
                        Instance.GlobalStorePlayers.Add(playerdata);
                    }

                    string finalTargetName = (!string.IsNullOrEmpty(playerdata.PlayerName) &&
                        playerdata.PlayerName != steamId.SteamId64.ToString())
                            ? playerdata.PlayerName
                            : steamId.SteamId64.ToString();

                    return new TargetFind { StorePlayer = playerdata, TargetName = finalTargetName };
                }
            }
            command.ReplyToCommand($"{Config.Settings.Tag}{Instance.Localizer["No matching client"]}");
            return new TargetFind();
        }

        if (singleTarget && targetResult.Players.Count > 1)
        {
            command.ReplyToCommand($"{Config.Settings.Tag}{Instance.Localizer["More than one client matched"]}");
            return new TargetFind();
        }

        string targetName = targetResult.Players.Count == 1
            ? targetResult.Players[0].PlayerName
            : GetTargetGroupName(command.GetArg(1), targetResult);

        return new TargetFind { Players = targetResult.Players, TargetName = targetName };
    }

    public static CCSPlayerController? FindTargetFromWeapon(CBasePlayerWeapon weapon)
    {
        SteamID steamId = new(weapon.OriginalOwnerXuidLow);

        CCSPlayerController? player = steamId.IsValid()
                ? Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == steamId.SteamId64) ?? Utilities.GetPlayerFromSteamId(weapon.OriginalOwnerXuidLow)
        : Utilities.GetPlayerFromIndex((int)weapon.OwnerEntity.Index) ?? Utilities.GetPlayerFromIndex((int)weapon.As<CCSWeaponBaseGun>().OwnerEntity.Value!.Index);

        return !string.IsNullOrEmpty(player?.PlayerName) ? player : null;
    }

    private static string GetTargetGroupName(string targetArg, TargetResult targetResult)
    {
        return !Target.TargetTypeMap.TryGetValue(targetArg, out TargetType type)
            ? targetResult.Players.First().PlayerName
            : type switch
            {
                TargetType.GroupAll => Instance.Localizer["all"],
                TargetType.GroupBots => Instance.Localizer["bots"],
                TargetType.GroupHumans => Instance.Localizer["humans"],
                TargetType.GroupAlive => Instance.Localizer["alive"],
                TargetType.GroupDead => Instance.Localizer["dead"],
                TargetType.GroupNotMe => Instance.Localizer["notme"],
                TargetType.PlayerMe => targetResult.Players.First().PlayerName,
                TargetType.TeamCt => Instance.Localizer["ct"],
                TargetType.TeamT => Instance.Localizer["t"],
                TargetType.TeamSpec => Instance.Localizer["spec"],
                _ => targetResult.Players.First().PlayerName
            };
    }
}