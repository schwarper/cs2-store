using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using System.Collections.Generic;
using System.Linq;

namespace Store
{
    public static class FindTarget
    {
        public static (List<CCSPlayerController> players, string targetName) Find(CommandInfo command, int minArgCount, bool singleTarget, bool ignoreMessage = false)
        {
            if (command.ArgCount < minArgCount)
            {
                return (new List<CCSPlayerController>(), string.Empty);
            }

            TargetResult targetResult = command.GetArgTargetResult(1);

            if (targetResult.Players.Count == 0)
            {
                if (!ignoreMessage)
                {
                    command.ReplyToCommand(Instance.Config.Tag + Instance.Localizer["No matching client"]);
                }

                return (new List<CCSPlayerController>(), string.Empty);
            }
            else if (singleTarget && targetResult.Players.Count > 1)
            {
                command.ReplyToCommand(Instance.Config.Tag + Instance.Localizer["More than one client matched"]);
                return (new List<CCSPlayerController>(), string.Empty);
            }

            string targetName;

            if (targetResult.Players.Count == 1)
            {
                targetName = targetResult.Players.Single().PlayerName;
            }
            else
            {
                Target.TargetTypeMap.TryGetValue(command.GetArg(1), out TargetType type);

                targetName = type switch
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

            return (targetResult.Players, targetName);
        }
    }
}
