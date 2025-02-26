using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using static Store.Config_Config;
using static Store.Store;

namespace Store;

public static class Command
{
    public static void Load()
    {
        Config_Commands config = Config.Commands;

        Dictionary<IEnumerable<string>, (string description, CommandInfo.CommandCallback handler)> commands = new()
        {
            {config.Credits, ("Show credits", Command_Credits)},
            {config.Store, ("Store menu", Command_Store)},
            {config.Inventory, ("Open inventory menu", Command_Inv)},
            {config.GiveCredits, ("Give credits", Command_GiveCredits)},
            {config.Gift, ("Gift", Command_Gift)},
            {config.ResetPlayer, ("Reset player's inventory", Command_ResetPlayer)},
            {config.ResetDatabase, ("Reset database", Command_ResetDatabase)},
            {config.RefreshPlayersCredits, ("Refresh players' credits", Command_RefreshPlayersCredits)}
        };

        foreach (KeyValuePair<IEnumerable<string>, (string description, CommandInfo.CommandCallback handler)> commandPair in commands)
        {
            foreach (string command in commandPair.Key)
            {
                Instance.AddCommand($"css_{command}", commandPair.Value.description, commandPair.Value.handler);
            }
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
    public static void Command_Store(CCSPlayerController? player, CommandInfo command) =>
        Menu.DisplayStoreMenu(player, false);

    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public static void Command_Inv(CCSPlayerController? player, CommandInfo command) =>
        Menu.DisplayStoreMenu(player, true);

    [CommandHelper(minArgs: 2, "<name, #userid, all @ commands> <credits>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public static void Command_GiveCredits(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget.Find(command, 2, false, true);

        if (players == null)
        {
            if (!SteamID.TryParse(command.GetArg(1), out SteamID? steamId) || steamId == null)
            {
                command.ReplyToCommand(Config.Tag + Instance.Localizer["Must be a steamid"]);
                return;
            }

            if (!int.TryParse(command.GetArg(2), out int credits))
            {
                command.ReplyToCommand(Config.Tag + Instance.Localizer["Must be an integer"]);
                return;
            }

            StoreApi.Store.Store_Player? playerdata = Instance.GlobalStorePlayers.SingleOrDefault(player => player.SteamID == steamId.SteamId64);

            if (playerdata == null)
            {
                command.ReplyToCommand(Config.Tag + Instance.Localizer["No matching client"]);
                return;
            }

            playerdata.Credits += credits;

            Database.ExecuteAsync("UPDATE store_players SET Credits = Credits + @Credits WHERE SteamId = @SteamId;", new { Credits = credits, @SteamID = steamId.SteamId64 });

            Server.PrintToChatAll(Config.Tag + Instance.Localizer["css_givecredits<steamid>", player?.PlayerName ?? "Console", steamId.SteamId64, credits]);
            return;
        }

        if (!int.TryParse(command.GetArg(2), out int value))
        {
            command.ReplyToCommand(Config.Tag + Instance.Localizer["Must be an integer"]);
            return;
        }

        foreach (CCSPlayerController target in players)
        {
            Credits.Give(target, value);
        }

        if (players.Count == 1)
        {
            Server.PrintToChatAll(Config.Tag + Instance.Localizer["css_givecredits<player>", player?.PlayerName ?? "Console", targetname, value]);
        }
        else
        {
            Server.PrintToChatAll(Config.Tag + Instance.Localizer["css_givecredits<multiple>", player?.PlayerName ?? "Console", targetname, value]);
        }
    }

    [CommandHelper(minArgs: 2, "<name, #userid> <credits>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public static void Command_Gift(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            return;
        }

        if (Instance.GlobalGiftTimeout[player] > Server.CurrentTime)
        {
            command.ReplyToCommand(Config.Tag + Instance.Localizer["Gift timeout", Math.Ceiling(Instance.GlobalGiftTimeout[player] - Server.CurrentTime)]);
            return;
        }

        (List<CCSPlayerController> players, _) = FindTarget.Find(command, 2, false);

        if (players == null)
        {
            return;
        }

        if (players.Count > 1)
        {
            command.ReplyToCommand(Config.Tag + Instance.Localizer["More than one client matched"]);
            return;
        }

        CCSPlayerController target = players.Single();

        if (target == player)
        {
            command.ReplyToCommand(Config.Tag + Instance.Localizer["No gift yourself"]);
            return;
        }

        if (!int.TryParse(command.GetArg(2), out int value))
        {
            command.ReplyToCommand(Config.Tag + Instance.Localizer["Must be an integer"]);
            return;
        }

        if (value <= 0)
        {
            command.ReplyToCommand(Config.Tag + Instance.Localizer["Must be higher than zero"]);
            return;
        }

        if (Credits.Get(player) < value)
        {
            player.PrintToChatMessage("No credits enough");
            return;
        }

        Credits.Give(player, -value);
        Credits.Give(target, value);

        Instance.GlobalGiftTimeout[player] = Server.CurrentTime + 5.0f;

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

        Instance.GlobalStorePlayers.Clear();
        Instance.GlobalStorePlayerItems.Clear();
        Instance.GlobalStorePlayerEquipments.Clear();

        Database.ResetDatabase();

        foreach (CCSPlayerController target in Utilities.GetPlayers())
        {
            Database.LoadPlayer(target);
        }
    }

    [CommandHelper(minArgs: 1, whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public static void Command_ResetPlayer(CCSPlayerController? player, CommandInfo command)
    {
        (List<CCSPlayerController> players, string targetname) = FindTarget.Find(command, 2, false);

        if (players == null)
        {
            if (!SteamID.TryParse(command.GetArg(1), out SteamID? steamId) || steamId == null)
            {
                command.ReplyToCommand(Config.Tag + Instance.Localizer["Must be a steamid"]);
                return;
            }

            StoreApi.Store.Store_Player? playerdata = Instance.GlobalStorePlayers.SingleOrDefault(player => player.SteamID == steamId.SteamId64);

            if (playerdata == null)
            {
                command.ReplyToCommand(Config.Tag + Instance.Localizer["No matching client"]);
                return;
            }

            Instance.GlobalStorePlayers.RemoveAll(p => p.SteamID == steamId.SteamId64);
            Instance.GlobalStorePlayerItems.RemoveAll(p => p.SteamID == steamId.SteamId64);
            Instance.GlobalStorePlayerEquipments.RemoveAll(p => p.SteamID == steamId.SteamId64);

            Database.ExecuteAsync("DELETE FROM store_players WHERE SteamId = @SteamId; " +
                 "DELETE FROM store_items WHERE SteamId = @SteamId; " +
                 "DELETE FROM store_equipments WHERE SteamId = @SteamId;",
                 new { @SteamID = steamId.SteamId64 });


            Server.PrintToChatAll(Config.Tag + Instance.Localizer["css_reset", player?.PlayerName ?? "Console", steamId.SteamId64]);
            return;
        }

        if (players.Count > 1)
        {
            command.ReplyToCommand(Config.Tag + Instance.Localizer["More than one client matched"]);
            return;
        }

        CCSPlayerController target = players.Single();

        Credits.Set(target, 0);
        Instance.GlobalStorePlayerItems.RemoveAll(p => p.SteamID == target.SteamID);
        Instance.GlobalStorePlayerEquipments.RemoveAll(p => p.SteamID == target.SteamID);

        Database.ResetPlayer(target);

        Server.PrintToChatAll(Config.Tag + Instance.Localizer["css_reset", player?.PlayerName ?? "Console", target.PlayerName]);
    }

    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    public static void Command_RefreshPlayersCredits(CCSPlayerController? player, CommandInfo info)
    {
        var players = Utilities.GetPlayers();

        foreach (var target in players)
        {
            Database.SavePlayer(target);
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(Config.Tag + Instance.Localizer["Players' credits are refreshed"]);
        Console.ResetColor();
    }
}