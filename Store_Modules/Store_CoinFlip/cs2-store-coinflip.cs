using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using StoreApi;
using System.Drawing;
using System.Text;
using System.Text.Json.Serialization;

namespace Store_CoinFlip;

public class Store_CoinFlipConfig : BasePluginConfig
{
    [JsonPropertyName("enable_duel")]
    public bool EnableDuel { get; set; } = true;

    [JsonPropertyName("enable_coinflip_start_of_round")]
    public bool EnableCoinflipStartOfRound { get; set; } = true;

    [JsonPropertyName("min_coinflip")]
    public int MinCoinFlip { get; set; } = 0;

    [JsonPropertyName("max_coinflip")]
    public int MaxCoinFlip { get; set; } = 1000;

    [JsonPropertyName("min_duel")]
    public int MinDuel { get; set; } = 0;

    [JsonPropertyName("max_duel")]
    public int MaxDuel { get; set; } = 1000;

    [JsonPropertyName("coinflipBet_commands")]
    public List<string> BetCommands { get; set; } = ["coinflip", "cf"];

    [JsonPropertyName("coinflipSendDuel_commands")]
    public List<string> SendDuelCommands { get; set; } = ["coinflipduel", "cfduel"];

    [JsonPropertyName("coinflipAcceptDuel_commands")]
    public List<string> AcceptDuelCommands { get; set; } = ["coinflipduelaccept", "cfduelaccept"];

    [JsonPropertyName("coinflipDeclineDuel_commands")]
    public List<string> DeclineDuelCommands { get; set; } = ["coinflipdueldecline", "cfdueldecline"];

    [JsonPropertyName("heads")]
    public Dictionary<string, int> Heads { get; set; } = new Dictionary<string, int>
    {
        { "multiplier", 2 },
        { "probability", 50 }
    };

    [JsonPropertyName("tails")]
    public Dictionary<string, int> Tails { get; set; } = new Dictionary<string, int>
    {
        { "multiplier", 2 },
        { "probability", 50 }
    };
    
    [JsonPropertyName("gif_enable")]
    public bool ShowWinnerGif { get; set; } = true;

    [JsonPropertyName("heads_gif_or_image")]
    public string HeadsImage { get; set; } = "https://c.tenor.com/nEu74vu_sT4AAAAC/tenor.gif";

    [JsonPropertyName("tails_gif_or_image")]
    public string TailsImage { get; set; } = "https://c.tenor.com/Gv5d5zs4sisAAAAC/tenor.gif";
    
    [JsonPropertyName("gif_display_duration")]
    public float GifDisplayDuration { get; set; } = 4.0f;

}

public class Store_CoinFlip : BasePlugin, IPluginConfig<Store_CoinFlipConfig>
{
    public override string ModuleName => "[Store Module] CoinFlip";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "Nathy & Schwarper";

    public IStoreApi? StoreApi { get; set; }
    public Dictionary<string, Dictionary<CCSPlayerController, int>> GlobalCoinFlip { get; set; } = new();
    public Random Random { get; set; } = new();
    public Store_CoinFlipConfig Config { get; set; } = new Store_CoinFlipConfig();
    public List<string> Options = new() { "Heads", "Tails" };
    private string? winnerOption = null;
    private bool shouldShowWinnerGif = false;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        StoreApi = IStoreApi.Capability.Get() ?? throw new Exception("StoreApi could not be located.");

        CreateCommands();

        foreach (string option in Options)
        {
            GlobalCoinFlip.Add(option, new Dictionary<CCSPlayerController, int>());
        }

        RegisterListener<Listeners.OnTick>(OnTick);
    }

    private void CreateCommands()
    {
        foreach (var cmd in Config.BetCommands)
        {
            AddCommand($"css_{cmd}", "Coinflip Bet", Command_CoinFlip);
        }

        foreach (var cmd in Config.SendDuelCommands)
        {
            AddCommand($"css_{cmd}", "Coinflip send duel", Command_DuelCoinFlip);
        }

        foreach (var cmd in Config.AcceptDuelCommands)
        {
            AddCommand($"css_{cmd}", "Coinflip accept duel", Command_AcceptDuel);
        }

        foreach (var cmd in Config.DeclineDuelCommands)
        {
            AddCommand($"css_{cmd}", "Coinflip decline duel", Command_DeclineDuel);
        }
    }

    public void OnConfigParsed(Store_CoinFlipConfig config)
    {
        config.MinCoinFlip = Math.Max(0, config.MinCoinFlip);
        config.MaxCoinFlip = Math.Max(config.MinCoinFlip + 1, config.MaxCoinFlip);

        config.MinDuel = Math.Max(0, config.MinDuel);
        config.MaxDuel = Math.Max(config.MinDuel + 1, config.MaxDuel);

        static void UpdateMultiplierAndProbability(Dictionary<string, int> option)
        {
            option["multiplier"] = Math.Max(option["multiplier"], 1);
            option["probability"] = Math.Max(option["probability"], 1);
        }

        UpdateMultiplierAndProbability(config.Heads);
        UpdateMultiplierAndProbability(config.Tails);

        Config = config;
    }
    
    [CommandHelper(minArgs: 1, usage: "[nick] [amount]")]
    public void Command_DuelCoinFlip(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        if (StoreApi == null)
        {
            throw new Exception("StoreApi could not be located.");
        }

        if (!Config.EnableDuel)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Duel feature is disabled"]);
            return;
        }

        var targetResult = info.GetArgTargetResult(1);
        var targetPlayer = targetResult.Players.FirstOrDefault();

        if (targetPlayer == player)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Cannot challenge yourself"]);
            return;
        }

        if (targetPlayer == null || !targetPlayer.IsValid || targetPlayer.IsBot || targetPlayer.IsHLTV)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Invalid target player"]);
            return;
        }

        if (!int.TryParse(info.GetArg(2), out int credits))
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be an integer"]);
            return;
        }

        if (credits < Config.MinDuel)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Min coinflip", Config.MinDuel]);
            return;
        }

        if (credits > Config.MaxDuel)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Max coinflip", Config.MaxDuel]);
            return;
        }

        if (StoreApi.GetPlayerCredits(player) < credits)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["No enough credits"]);
            return;
        }

        if (StoreApi.GetPlayerCredits(targetPlayer) < credits)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Target player has no enough credits"]);
            return;
        }

        SendDuelInvite(player, targetPlayer, credits);
    }

    public void SendDuelInvite(CCSPlayerController challenger, CCSPlayerController target, int credits)
    {
        challenger.PrintToChat(Localizer["Prefix"] + Localizer["Duel request sent", target.PlayerName, credits]);
        target.PrintToChat(Localizer["Prefix"] + Localizer["Duel request", challenger.PlayerName, credits]);

        PendingDuels[target] = new DuelInfo
        {
            Challenger = challenger,
            Credits = credits
        };
    }

    public void Command_AcceptDuel(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        if (!PendingDuels.TryGetValue(player, out var duelInfo))
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["No duel invitations"]);
            return;
        }

        StartDuel(player, duelInfo);
    }

    public void Command_DeclineDuel(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        if (PendingDuels.Remove(player))
        {
            player.PrintToChat(Localizer["Prefix"] + Localizer["You declined the duel"]);
        }
        else
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["No duel invitations"]);
        }
    }

    public void StartDuel(CCSPlayerController target, DuelInfo duelInfo)
    {
        var challenger = duelInfo.Challenger;
        var credits = duelInfo.Credits;

        if (StoreApi.GetPlayerCredits(challenger) < credits || StoreApi.GetPlayerCredits(target) < credits)
        {
            target.PrintToChat(Localizer["Prefix"] + Localizer["No enough credits"]);
            challenger.PrintToChat(Localizer["Prefix"] + Localizer["No enough credits"]);
            return;
        }

        StoreApi.GivePlayerCredits(challenger, -credits);
        StoreApi.GivePlayerCredits(target, -credits);

        string winnerOption = Random.Next(2) == 0 ? "Heads" : "Tails";
        CCSPlayerController winner = winnerOption == "Heads" ? challenger : target;
        CCSPlayerController loser = winner == challenger ? target : challenger;

        int multiplier = FindMultiplier(winnerOption);
        StoreApi.GivePlayerCredits(winner, credits * multiplier);

        winner.PrintToChat(Localizer["Prefix"] + Localizer["Duel win message", loser.PlayerName, credits * multiplier]);

        loser.PrintToChat(Localizer["Prefix"] + Localizer["Duel lose message", winner.PlayerName, credits]);

        PendingDuels.Remove(target);
    }

    private Dictionary<CCSPlayerController, DuelInfo> PendingDuels = new();

    public class DuelInfo
    {
        public CCSPlayerController Challenger { get; set; }
        public int Credits { get; set; }
    }


    public void Command_CoinFlip(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        if (StoreApi == null)
        {
            throw new Exception("StoreApi could not be located.");
        }

        if (!Config.EnableCoinflipStartOfRound)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Coinflip feature is disabled"]);
            return;
        }

        if (Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!.WarmupPeriod)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Cannot join during warmup"]);
            return;
        }

        if (GlobalCoinFlip.Values.Any(dict => dict.ContainsKey(player)))
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["You are already in"]);
            return;
        }

        if (!int.TryParse(info.GetArg(1), out int credits))
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be an integer"]);
            return;
        }

        if (StoreApi.GetPlayerCredits(player) < credits)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["No enough credits"]);
            return;
        }

        if (credits < Config.MinCoinFlip)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Min coinflip", Config.MinCoinFlip]);
            return;
        }

        if (credits > Config.MaxCoinFlip)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Max coinflip", Config.MaxCoinFlip]);
            return;
        }

        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            StringBuilder builder = new();
            builder.AppendFormat(Localizer["menu_title", credits]);

            CenterHtmlMenu menu = new(builder.ToString(), this)
            {
                PostSelectAction = PostSelectAction.Close
            };

            AddCoinFlipOption(menu, info, credits, "Heads", Config.Heads["multiplier"]);
            AddCoinFlipOption(menu, info, credits, "Tails", Config.Tails["multiplier"]);

            MenuManager.OpenCenterHtmlMenu(this, player, menu);
        }
    }

    public void AddCoinFlipOption(CenterHtmlMenu menu, CommandInfo info, int credits, string option, int multiplier)
    {
        StringBuilder builder = new();
        builder.AppendFormat(Localizer["menu_options", Localizer[option], multiplier]);

        menu.AddMenuOption(builder.ToString(), (player, menuOption) =>
        {
            JoinCoinFlip(player, info, credits, option);
        });
    }

    public void JoinCoinFlip(CCSPlayerController player, CommandInfo info, int credits, string option)
    {
        if (StoreApi == null)
        {
            throw new Exception("StoreApi could not be located.");
        }

        if (StoreApi.GetPlayerCredits(player) < credits)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["No enough credits"]);
            return;
        }

        StoreApi.GivePlayerCredits(player, -credits);
        GlobalCoinFlip[option].Add(player, credits);

        PrintToChatAll(Localizer["Join coinflip"], player.PlayerName, credits, Localizer[option]);
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (StoreApi == null)
        {
            throw new Exception("StoreApi could not be located.");
        }

        if (Config.EnableCoinflipStartOfRound)
        {
            PrintToChatAll(Localizer["Announce coinflip"]);
            Pay();
        }

        foreach (string option in Options)
        {
            GlobalCoinFlip[option].Clear();
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (!Config.EnableCoinflipStartOfRound)
        {
            return HookResult.Continue;
        }

        int totalProbability = Config.Heads["probability"] + Config.Tails["probability"];
        int randomNumber = Random.Next(1, totalProbability + 1);

        if (randomNumber <= Config.Heads["probability"])
        {
            GiveCreditsToWinner("Heads");
            winnerOption = "Heads";
        }
        else
        {
            GiveCreditsToWinner("Tails");
            winnerOption = "Tails";
        }

        shouldShowWinnerGif = true;

        AddTimer(Config.GifDisplayDuration, () => 
        {
            shouldShowWinnerGif = false;
            winnerOption = null;
        });

        return HookResult.Continue;
    }

    public void GiveCreditsToWinner(string option)
    {
        if (StoreApi == null)
        {
            throw new Exception("StoreApi could not be located.");
        }

        PrintToChatAll(Localizer["Winner coinflip"], Localizer[option]);
        Pay(option);
        
        winnerOption = option;

        foreach (string opt in Options)
        {
            GlobalCoinFlip[opt].Clear();
        }
    }

    public void OnTick()
    {
        if (shouldShowWinnerGif && winnerOption != null && Config.ShowWinnerGif && Config.EnableCoinflipStartOfRound)
        {
            string gifUrl = winnerOption == "Heads" ? Config.HeadsImage : Config.TailsImage;

            foreach (var player in Utilities.GetPlayers())
            {
                if (player != null && player.IsValid)
                {
                    player.PrintToCenterHtml($"<img src=\"{gifUrl}\">");
                }
            }
        }
    }


    public void Pay(string option)
    {
        int multiplier = FindMultiplier(option);

        foreach (KeyValuePair<CCSPlayerController, int> kv in GlobalCoinFlip[option])
        {
            CCSPlayerController player = kv.Key;
            int credits = kv.Value;

            if (player == null || !player.IsValid)
            {
                continue;
            }

            StoreApi!.GivePlayerCredits(player, credits * multiplier);
        }
    }

    public void Pay()
    {
        foreach (KeyValuePair<string, Dictionary<CCSPlayerController, int>> kv in GlobalCoinFlip)
        {
            string option = kv.Key;
            Dictionary<CCSPlayerController, int> dictionary = kv.Value;

            foreach (KeyValuePair<CCSPlayerController, int> kvp in dictionary)
            {
                CCSPlayerController player = kvp.Key;
                int credits = kvp.Value;

                if (player == null || !player.IsValid)
                {
                    continue;
                }

                StoreApi!.GivePlayerCredits(player, credits);
            }
        }
    }

    public int FindMultiplier(string option)
    {
        return option switch
        {
            "Heads" => Config.Heads["multiplier"],
            "Tails" => Config.Tails["multiplier"],
            _ => 1
        };
    }

    public void PrintToChatAll(string message, params object[] args)
    {
        foreach (CCSPlayerController player in Utilities.GetPlayers())
        {
            using (new WithTemporaryCulture(player.GetLanguage()))
            {
                StringBuilder builder = new(Localizer["Prefix"]);
                builder.AppendFormat(Localizer[message], args);
                player.PrintToChat(builder.ToString());
            }
        }
    }
}
