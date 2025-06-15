using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Store.Extension;
using static Store.Config_Config;
using static Store.FindTarget;
using static Store.Store;

namespace Store;

public static class Command
{
    public static void Load()
    {
        Config_Commands config = Config.Commands;

        Dictionary<IEnumerable<string>, (string Description, CommandInfo.CommandCallback Handler)> commands = new()
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

        foreach ((IEnumerable<string> commandList, (string description, CommandInfo.CommandCallback handler)) in commands)
        {
            foreach (string command in commandList)
            {
                Instance.AddCommand($"css_{command}", description, handler);
            }
        }
    }

    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public static void Command_Credits(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null) return;

        player.PrintToChatMessage("css_credits", Credits.Get(player));
    }

    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public static void Command_Store(CCSPlayerController? player, CommandInfo command)
    {
        MenuBase.DisplayStoreMenu(player, false);
    }

    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public static void Command_Inv(CCSPlayerController? player, CommandInfo command)
    {
        MenuBase.DisplayStoreMenu(player, true);
    }

    [CommandHelper(minArgs: 2, "<name, #userid, all @ commands> <credits>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public static void Command_GiveCredits(CCSPlayerController? player, CommandInfo command)
    {
        if (string.IsNullOrEmpty(Config.Permissions.GiveCredits) || !AdminManager.PlayerHasPermissions(player, Config.Permissions.GiveCredits))
            return;

        if (!int.TryParse(command.GetArg(2), out int credits))
        {
            command.ReplyToCommand($"{Config.Settings.Tag}{Instance.Localizer["Must be an integer"]}");
            return;
        }

        TargetFind target = Find(command, false, true);

        if (string.IsNullOrEmpty(target.TargetName)) return;

        if (target.StorePlayer != null)
        {
            target.StorePlayer.Credits += credits;

            Database.ExecuteAsync("UPDATE store_players SET Credits = Credits + @Credits WHERE SteamId = @SteamId;", new { Credits = credits, SteamId = target.StorePlayer.SteamID });

            Server.PrintToChatAll($"{Config.Settings.Tag}{Instance.Localizer["css_givecredits<steamid>", player?.PlayerName ?? "Console", target.TargetName, credits]}");
            return;
        }

        foreach (CCSPlayerController targetPlayer in target.Players)
        {
            Credits.Give(targetPlayer, credits);
        }

        Server.PrintToChatAll($"{Config.Settings.Tag}{Instance.Localizer[target.Players.Count == 1 ? "css_givecredits<player>" : "css_givecredits<multiple>", player?.PlayerName ?? "Console", target.TargetName, credits]}");
    }

    [CommandHelper(minArgs: 2, "<name, #userid> <credits>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public static void Command_Gift(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null) return;

        if (Instance.GlobalGiftTimeout[player] > Server.CurrentTime)
        {
            command.ReplyToCommand($"{Config.Settings.Tag}{Instance.Localizer["Gift timeout", Math.Ceiling(Instance.GlobalGiftTimeout[player] - Server.CurrentTime)]}");
            return;
        }

        if (!int.TryParse(command.GetArg(2), out int value))
        {
            command.ReplyToCommand($"{Config.Settings.Tag}{Instance.Localizer["Must be an integer"]}");
            return;
        }

        if (value <= 0)
        {
            command.ReplyToCommand($"{Config.Settings.Tag}{Instance.Localizer["Must be higher than zero"]}");
            return;
        }

        TargetFind target = Find(command, true, false);

        if (string.IsNullOrEmpty(target.TargetName)) return;

        CCSPlayerController targetPlayer = target.Players[0];

        if (targetPlayer == player)
        {
            command.ReplyToCommand($"{Config.Settings.Tag}{Instance.Localizer["No gift yourself"]}");
            return;
        }

        if (Credits.Get(player) < value)
        {
            player.PrintToChatMessage("No credits enough");
            return;
        }

        Credits.Give(player, -value);
        Credits.Give(targetPlayer, value);

        Instance.GlobalGiftTimeout[player] = Server.CurrentTime + 5.0f;

        player.PrintToChatMessage("css_gift<player>", targetPlayer.PlayerName, value);
        targetPlayer.PrintToChatMessage("css_gift<target>", player.PlayerName, value);
    }

    [CommandHelper(minArgs: 0, whoCanExecute: CommandUsage.SERVER_ONLY)]
    public static void Command_ResetDatabase(CCSPlayerController? player, CommandInfo command)
    {
        if (player != null) return;

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
        TargetFind target = Find(command, true, true);

        if (string.IsNullOrEmpty(target.TargetName)) return;

        if (target.StorePlayer != null)
        {
            Instance.GlobalStorePlayers.RemoveAll(p => p.SteamID == target.StorePlayer.SteamID);
            Instance.GlobalStorePlayerItems.RemoveAll(p => p.SteamID == target.StorePlayer.SteamID);
            Instance.GlobalStorePlayerEquipments.RemoveAll(p => p.SteamID == target.StorePlayer.SteamID);

            Database.ExecuteAsync(
                $"DELETE FROM {Config.DatabaseConnection.StorePlayersName} WHERE SteamId = @SteamId; " +
                $"DELETE FROM {Config.DatabaseConnection.StoreItemsName} WHERE SteamId = @SteamId; " +
                $"DELETE FROM {Config.DatabaseConnection.StoreEquipments} WHERE SteamId = @SteamId;",
                new { target.StorePlayer.SteamID });

            Server.PrintToChatAll($"{Config.Settings.Tag}{Instance.Localizer["css_reset", player?.PlayerName ?? "Console", target.StorePlayer.SteamID]}");
            return;
        }

        CCSPlayerController targetPlayer = target.Players[0];

        Credits.Set(targetPlayer, 0);
        Instance.GlobalStorePlayerItems.RemoveAll(p => p.SteamID == targetPlayer.SteamID);
        Instance.GlobalStorePlayerEquipments.RemoveAll(p => p.SteamID == targetPlayer.SteamID);

        Database.ResetPlayer(targetPlayer);

        Server.PrintToChatAll($"{Config.Settings.Tag}{Instance.Localizer["css_reset", player?.PlayerName ?? "Console", targetPlayer.PlayerName]}");
    }

    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    public static void Command_RefreshPlayersCredits(CCSPlayerController? player, CommandInfo info)
    {
        foreach (CCSPlayerController target in Utilities.GetPlayers())
        {
            Database.SavePlayer(target);
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{Config.Settings.Tag}{Instance.Localizer["Players' credits are refreshed"]}");
        Console.ResetColor();
    }
}
