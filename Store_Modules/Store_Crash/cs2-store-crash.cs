using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using StoreApi;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace Store_Crash;

public class Store_CrashConfig : BasePluginConfig
{
    [JsonPropertyName("min_bet")]
    public int MinBet { get; set; } = 10;

    [JsonPropertyName("max_bet")]
    public int MaxBet { get; set; } = 1000;

    [JsonPropertyName("min_multiplier")]
    public float MinMultiplier { get; set; } = 1.1f;

    [JsonPropertyName("max_multiplier")]
    public float MaxMultiplier { get; set; } = 9.9f;

    [JsonPropertyName("multiplier_increment")]
    public float MultiplierIncrement { get; set; } = 0.01f;

    [JsonPropertyName("crash_commands")]
    public List<string> CrashCommands { get; set; } = ["crash"];
}

public class CrashGame
{
    public CCSPlayerController Player { get; set; }
    public int BetCredits { get; set; }
    public float TargetMultiplier { get; set; }
    public float CurrentMultiplier { get; set; }
    public bool IsActive { get; set; }
    public float CrashMultiplier { get; set; }

    public CrashGame(CCSPlayerController player, int betCredits, float targetMultiplier, float crashMultiplier)
    {
        Player = player;
        BetCredits = betCredits;
        TargetMultiplier = targetMultiplier;
        CurrentMultiplier = 0.0f;
        IsActive = true;
        CrashMultiplier = crashMultiplier;
    }
}

public class Store_Crash : BasePlugin, IPluginConfig<Store_CrashConfig>
{
    public override string ModuleName => "Store Module [Crash]";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "Nathy";

    private readonly Random random = new();
    public IStoreApi? StoreApi { get; set; }
    public Store_CrashConfig Config { get; set; } = new();
    private readonly ConcurrentDictionary<string, CrashGame> activeGames = new();

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        StoreApi = IStoreApi.Capability.Get() ?? throw new Exception("StoreApi could not be located.");
        CreateCommands();
        RegisterListener<Listeners.OnTick>(OnTick);
    }

    public void OnConfigParsed(Store_CrashConfig config)
    {
        config.MinBet = Math.Max(0, config.MinBet);
        config.MaxBet = Math.Max(config.MinBet + 1, config.MaxBet);

        Config = config;
    }

    private void CreateCommands()
    {
        foreach (var cmd in Config.CrashCommands)
        {
            AddCommand($"css_{cmd}", "Start a crash bet", Command_Crash);
        }
    }

    [CommandHelper(minArgs: 2, usage: "<credits> <multiplier>")]
    public void Command_Crash(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (StoreApi == null) throw new Exception("StoreApi could not be located.");

        if (!int.TryParse(info.GetArg(1), out int credits))
        {
            info.ReplyToCommand(Localizer["Invalid amount of credits"]);
            return;
        }

        if (!float.TryParse(info.GetArg(2), out float targetMultiplier))
        {
            info.ReplyToCommand(Localizer["Invalid multiplier"]);
            return;
        }

        if (credits < Config.MinBet)
        {
            info.ReplyToCommand(Localizer["Minimum bet amount", Config.MinBet]);
            return;
        }

        if (credits > Config.MaxBet)
        {
            info.ReplyToCommand(Localizer["Maximum bet amount", Config.MaxBet]);
            return;
        }

        if (targetMultiplier < Config.MinMultiplier || targetMultiplier > Config.MaxMultiplier)
        {
            info.ReplyToCommand(Localizer["Multiplier range", Config.MinMultiplier, Config.MaxMultiplier]);
            return;
        }

        if (StoreApi.GetPlayerCredits(player) < credits)
        {
            info.ReplyToCommand(Localizer["Not enough credits"]);
            return;
        }

        float crashMultiplier = SimulateCrashMultiplier();
        StartCrashGame(player, credits, targetMultiplier, crashMultiplier);
    }

    private void StartCrashGame(CCSPlayerController player, int credits, float targetMultiplier, float crashMultiplier)
    {
        StoreApi.GivePlayerCredits(player, -credits);
        player.PrintToChat(Localizer["Bet placed", credits, targetMultiplier]);

        var game = new CrashGame(player, credits, targetMultiplier, crashMultiplier);
        activeGames[player.SteamID.ToString()] = game;
    }

    private void OnTick()
    {
        foreach (var game in activeGames.Values.ToList())
        {
            if (!game.IsActive) continue;

            game.CurrentMultiplier += Config.MultiplierIncrement;

            game.Player.PrintToCenter(Localizer["Current multiplier"] + $"{game.CurrentMultiplier:0.00}");

            if (game.CurrentMultiplier >= game.CrashMultiplier)
            {
                EndCrashGame(game);
            }
        }
    }

    private void EndCrashGame(CrashGame game)
    {
        if (game == null) return;

        float actualMultiplier = game.CurrentMultiplier;
        float targetMultiplier = game.TargetMultiplier;

        game.Player.PrintToCenter(Localizer["Multiplier crashed"] + $"{actualMultiplier:0.00}");

        if (actualMultiplier >= targetMultiplier)
        {
            int winnings = (int)(game.BetCredits * targetMultiplier);
            StoreApi.GivePlayerCredits(game.Player, winnings);
            game.Player.PrintToChat(Localizer["Bet win", winnings.ToString(), targetMultiplier.ToString("0.00"), actualMultiplier.ToString("0.00")]);
        }
        else
        {
            game.Player.PrintToChat(Localizer["Bet lost", actualMultiplier.ToString("0.00"), targetMultiplier.ToString("0.00")]);
        }

        game.IsActive = false;
        activeGames.TryRemove(game.Player.SteamID.ToString(), out _);
    }

    private float SimulateCrashMultiplier()
    {
        int randomNumber = random.Next(1, 101);

        if (randomNumber <= 80)
        {
            return (float)Math.Round(1.0 + random.NextDouble(), 2);
        }
        else if (randomNumber <= 90)
        {
            return (float)Math.Round(2.0 + random.NextDouble(), 2);
        }
        else if (randomNumber <= 95)
        {
            return (float)Math.Round(3.0 + random.NextDouble(), 2);
        }
        else if (randomNumber <= 99)
        {
            return (float)Math.Round(5.0 + random.NextDouble(), 2);
        }
        else
        {
            return (float)Math.Round(6.0 + random.NextDouble() * 10, 2);
        }
    }
}
