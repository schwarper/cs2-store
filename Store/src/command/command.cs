using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using static Store.Store;

namespace Store;

public static class Command
{
    public static void Load()
    {
        StoreConfig config = Instance.Config;

        Dictionary<string, (string description, CommandInfo.CommandCallback handler)> commands = new()
        {
            {"credits", ("Show credits", Command_Credits)},
            {"store", ("Store menu", Command_Store)},
            {"inventory", ("Open inventory menu", Command_Inv)},
            {"givecredits", ("Give credits", Command_GiveCredits)},
            {"gift", ("Gift", Command_Gift)},
            {"resetplayer", ("Reset player's inventory", Command_ResetPlayer)},
            {"resetdatabase", ("Reset database", Command_ResetDatabase)}
        };

        foreach (var command in commands)
        {
            Instance.AddCommand($"css_{command.Key}", command.Value.description, command.Value.handler);
        }
    }

    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public static void Command_Credits(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            return;
        }

        player.PrintToChatMessage("css_credits", Credits.Get(player));
    }

    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public static void Command_Store(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            return;
        }

        Menu.DisplayStore(player, false);
    }

    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public static void Command_Inv(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            return;
        }

        Menu.DisplayStore(player, true);
    }

    [CommandHelper(minArgs: 2, "<name, #userid, all @ commands> <credits>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public static void Command_GiveCredits(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = Instance.FindTarget(command, 2, false);

        if (players == null)
        {
            return;
        }

        if (!int.TryParse(command.GetArg(2), out int value))
        {
            command.ReplyToCommand(Instance.Localizer["Prefix"] + Instance.Localizer["Must be an integer"]);
            return;
        }

        foreach (CCSPlayerController target in players)
        {
            Credits.Give(target, value);
        }

        if (players.Count == 1)
        {
            Server.PrintToChatAll(Instance.Localizer["Prefix"] + Instance.Localizer["css_givecredits<player>", player == null ? Instance.Localizer["Console"] : player.PlayerName, targetname, value]);
        }
        else
        {
            Server.PrintToChatAll(Instance.Localizer["Prefix"] + Instance.Localizer["css_givecredits<multiple>", player == null ? Instance.Localizer["Console"] : player.PlayerName, targetname, value]);
        }
    }

    [CommandHelper(minArgs: 2, "<name, #userid> <credits>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public static void Command_Gift(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            return;
        }

        (List<CCSPlayerController> players, _) = Instance.FindTarget(command, 2, false);

        if (players == null)
        {
            return;
        }

        if (players.Count > 1)
        {
            command.ReplyToCommand(Instance.Localizer["Prefix"] + Instance.Localizer["More than one client matched"]);
            return;
        }

        CCSPlayerController target = players.Single();

        if (target == player)
        {
            command.ReplyToCommand(Instance.Localizer["Prefix"] + Instance.Localizer["No gift yourself"]);
            return;
        }

        if (!int.TryParse(command.GetArg(2), out int value))
        {
            command.ReplyToCommand(Instance.Localizer["Prefix"] + Instance.Localizer["Must be an integer"]);
            return;
        }

        Credits.Give(player, -value);
        Credits.Give(target, value);

        player.PrintToChatMessage("css_gift<player>", target.PlayerName, value);
        target.PrintToChatMessage("css_gift<target>", player.PlayerName, value);
    }

    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.SERVER_ONLY)]
    public static void Command_ResetDatabase(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null)
        {
            return;
        }

        Database.ResetDatabase();
    }

    [CommandHelper(minArgs: 1, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public static void Command_ResetPlayer(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = Instance.FindTarget(command, 2, false);

        if (players == null)
        {
            return;
        }

        if (players.Count > 1)
        {
            command.ReplyToCommand(Instance.Localizer["Prefix"] + Instance.Localizer["More than one client matched"]);
            return;
        }

        CCSPlayerController target = players.Single();

        Credits.Set(target, 0);
        Instance.GlobalStorePlayerItems.RemoveAll(p => p.SteamID == target.SteamID);
        Instance.GlobalStorePlayerEquipments.RemoveAll(p => p.SteamID == target.SteamID);

        Server.PrintToChatAll(Instance.Localizer["Prefix"] + Instance.Localizer["css_reset", player?.PlayerName ?? "Console", target.PlayerName]);
    }
}