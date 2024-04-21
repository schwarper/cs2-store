using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;

namespace Store;

public partial class Store : BasePlugin
{
    public (List<CCSPlayerController> players, string targetname) FindTarget
        (
            CommandInfo command,
            int minArgCount,
            bool singletarget,
            bool ignoreMessage = false
        )
    {
        if (command.ArgCount < minArgCount)
        {
            return (new List<CCSPlayerController>(), string.Empty);
        }

        TargetResult targetresult = command.GetArgTargetResult(1);

        if (targetresult.Players.Count == 0)
        {
            if (!ignoreMessage)
            {
                command.ReplyToCommand(Localizer["Prefix"] + Localizer["No matching client"]);
            }

            return (new List<CCSPlayerController>(), string.Empty);
        }
        else if (singletarget && targetresult.Players.Count > 1)
        {
            command.ReplyToCommand(Localizer["Prefix"] + Localizer["More than one client matched"]);
            return (new List<CCSPlayerController>(), string.Empty);
        }

        string targetname;

        if (targetresult.Players.Count == 1)
        {
            targetname = targetresult.Players.Single().PlayerName;
        }
        else
        {
            Target.TargetTypeMap.TryGetValue(command.GetArg(1), out TargetType type);

            targetname = type switch
            {
                TargetType.GroupAll => Localizer["all"],
                TargetType.GroupBots => Localizer["bots"],
                TargetType.GroupHumans => Localizer["humans"],
                TargetType.GroupAlive => Localizer["alive"],
                TargetType.GroupDead => Localizer["dead"],
                TargetType.GroupNotMe => Localizer["notme"],
                TargetType.PlayerMe => targetresult.Players.First().PlayerName,
                TargetType.TeamCt => Localizer["ct"],
                TargetType.TeamT => Localizer["t"],
                TargetType.TeamSpec => Localizer["spec"],
                _ => targetresult.Players.First().PlayerName
            };
        }

        return (targetresult.Players, targetname);
    }
}