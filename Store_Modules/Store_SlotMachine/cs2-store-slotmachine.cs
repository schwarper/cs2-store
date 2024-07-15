using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using StoreApi;

public class Store_SlotMachineConfig : BasePluginConfig
{
    [JsonPropertyName("min_bet")]
    public int MinBet { get; set; } = 10;

    [JsonPropertyName("max_bet")]
    public int MaxBet { get; set; } = 1000;

    [JsonPropertyName("slot_machine_commands")]
    public List<string> SlotMachineCommands { get; set; } =  [ "slotmachine" ];

    [JsonPropertyName("reward_multipliers")]
    public Dictionary<string, SlotMachineSymbol> RewardMultipliers { get; set; } = new Dictionary<string, SlotMachineSymbol>
    {
        { "★", new SlotMachineSymbol { Multiplier = 10, Chance = 2 } },
        { "♞", new SlotMachineSymbol { Multiplier = 8, Chance = 3 } },
        { "⚓", new SlotMachineSymbol { Multiplier = 6, Chance = 3 } },
        { "☕", new SlotMachineSymbol { Multiplier = 5, Chance = 4 } },
        { "⚽", new SlotMachineSymbol { Multiplier = 4, Chance = 4 } },
        { "☀", new SlotMachineSymbol { Multiplier = 3, Chance = 5 } },
        { "☁", new SlotMachineSymbol { Multiplier = 2, Chance = 5 } },
        { "✿", new SlotMachineSymbol { Multiplier = 15, Chance = 1 } },
        { "☾", new SlotMachineSymbol { Multiplier = 20, Chance = 0.5 } }
    };

    [JsonPropertyName("slot_timers")]
    public SlotTimersConfig SlotTimers { get; set; } = new SlotTimersConfig();

    [JsonPropertyName("partial_win_percentage")]
    public int PartialWinPercentage { get; set; } = 50;

    [JsonIgnore]
    public List<string> Emojis => RewardMultipliers.Keys.ToList();
}

public class SlotTimersConfig
{
    [JsonPropertyName("first_stop")]
    public float FirstStop { get; set; } = 1.0f;

    [JsonPropertyName("second_stop")]
    public float SecondStop { get; set; } = 2.0f;

    [JsonPropertyName("third_stop")]
    public float ThirdStop { get; set; } = 3.0f;
}

public class SlotMachineSymbol
{
    [JsonPropertyName("multiplier")]
    public int Multiplier { get; set; }

    [JsonPropertyName("chance")]
    public double Chance { get; set; }
}

namespace Store_SlotMachine
{
    public class Store_SlotMachine : BasePlugin, IPluginConfig<Store_SlotMachineConfig>
    {
        public override string ModuleName => "Store Module [Slot Machine]";
        public override string ModuleVersion => "0.0.1";
        public override string ModuleAuthor => "Nathy";

        private readonly Random random = new Random();
        public IStoreApi? StoreApi { get; set; }
        public Store_SlotMachineConfig Config { get; set; } = new Store_SlotMachineConfig();
        private readonly ConcurrentDictionary<string, SlotMachineGame> activeGames = new ConcurrentDictionary<string, SlotMachineGame>();

        private List<string> emojis = [];

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            StoreApi = IStoreApi.Capability.Get() ?? throw new Exception("StoreApi could not be located.");

            emojis = Config.Emojis;

            CreateCommands();
            RegisterListener<Listeners.OnTick>(OnTick);
        }

        public void OnConfigParsed(Store_SlotMachineConfig config)
        {
            config.MinBet = Math.Max(0, config.MinBet);
            config.MaxBet = Math.Max(config.MinBet + 1, config.MaxBet);

            Config = config;
        }

        private void CreateCommands()
        {
            foreach (var cmd in Config.SlotMachineCommands)
            {
                AddCommand($"css_{cmd}", "Start a slot machine bet", Command_Slot);
            }
        }

        [CommandHelper(minArgs: 1, usage: "<bet_amount>")]
        public void Command_Slot(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null) return;

            if (StoreApi == null) throw new Exception("StoreApi could not be located.");

            if (!int.TryParse(info.GetArg(1), out int betAmount))
            {
                info.ReplyToCommand(Localizer["Prefix"] + Localizer["Invalid amount of credits"]);
                return;
            }

            if (betAmount < Config.MinBet)
            {
                info.ReplyToCommand(Localizer["Prefix"] + Localizer["Minimum bet amount", Config.MinBet]);
                return;
            }

            if (betAmount > Config.MaxBet)
            {
                info.ReplyToCommand(Localizer["Prefix"] + Localizer["Maximum bet amount", Config.MaxBet]);
                return;
            }

            if (StoreApi.GetPlayerCredits(player) < betAmount)
            {
                info.ReplyToCommand(Localizer["Prefix"] + Localizer["Not enough credits"]);
                return;
            }

            StartSlotGame(player, betAmount);
        }

        private void StartSlotGame(CCSPlayerController player, int betAmount)
        {
            StoreApi!.GivePlayerCredits(player, -betAmount);

            List<string> results = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                string symbol = GetRandomSymbol();
                results.Add(symbol);
            }

            var game = new SlotMachineGame(player, betAmount, results);
            activeGames[player.SteamID.ToString()] = game;

            game.IsInProgress = true;

            AddTimer(Config.SlotTimers.FirstStop, () => StopSlot(game, 0));
            AddTimer(Config.SlotTimers.SecondStop, () => StopSlot(game, 1));
            AddTimer(Config.SlotTimers.ThirdStop, () => StopSlot(game, 2));
        }

        private void StopSlot(SlotMachineGame game, int slotIndex)
        {
            if (game == null || !game.IsInProgress) return;

            game.StoppedSlots[slotIndex] = true;

            if (game.StoppedSlots.All(stopped => stopped))
            {
                EndSlotGame(game);
            }
        }

        private void EndSlotGame(SlotMachineGame game)
        {
            if (game == null || !game.IsInProgress) return;

            string resultString = string.Join(" ", game.Results);
            int multiplier = CalculateMultiplier(game.Results);
            int winnings = game.BetAmount * multiplier;

            if (multiplier == 1)
            {
                winnings = game.BetAmount * Config.PartialWinPercentage / 100;
            }

            StoreApi!.GivePlayerCredits(game.Player, winnings);

            if (multiplier > 1)
            {
                game.Player.PrintToChat(Localizer["Prefix"] + Localizer["Result", resultString]);
                game.Player.PrintToChat(Localizer["Prefix"] + Localizer["Winnings", winnings]);
            }
            else if (multiplier == 1)
            {
                game.Player.PrintToChat(Localizer["Prefix"] + Localizer["Result", resultString]);
                game.Player.PrintToChat(Localizer["Prefix"] + Localizer["2 Symbols", winnings]);
            }
            else
            {
                game.Player.PrintToChat(Localizer["Prefix"] + Localizer["Result", resultString]);
                game.Player.PrintToChat(Localizer["Prefix"] + Localizer["You lost your bet", game.BetAmount]);
            }

            game.Player.PrintToCenter(Localizer["Result", resultString]);

            activeGames.TryRemove(game.Player.SteamID.ToString(), out _);

            game.IsInProgress = false;
        }

        private int CalculateMultiplier(List<string> results)
        {
            if (results.All(s => s == results[0]))
            {
                return Config.RewardMultipliers[results[0]].Multiplier;
            }
            else if (results.Distinct().Count() == 2)
            {
                return 1;
            }
            return 0;
        }

        private string GetRandomSymbol()
        {
            double totalChance = Config.RewardMultipliers.Sum(kv => kv.Value.Chance);
            double randomNumber = random.NextDouble() * totalChance;

            foreach (var symbol in Config.RewardMultipliers)
            {
                if (randomNumber < symbol.Value.Chance)
                {
                    return symbol.Key;
                }
                randomNumber -= symbol.Value.Chance;
            }

            return Config.RewardMultipliers.Last().Key;
        }

        private void OnTick()
        {
            foreach (var game in activeGames.Values.ToList())
            {
                if (game.IsInProgress)
                {
                    for (int i = 0; i < game.Results.Count; i++)
                    {
                        if (!game.StoppedSlots[i])
                        {
                            game.Results[i] = GetRandomSymbol();
                        }
                    }

                    string resultString = string.Join(" ", game.Results);
                    game.Player.PrintToCenter(resultString);
                }
            }
        }
    }

    public class SlotMachineGame
    {
        public CCSPlayerController Player { get; set; }
        public int BetAmount { get; set; }
        public List<string> Results { get; set; }
        public bool IsInProgress { get; set; }
        public bool[] StoppedSlots { get; set; }

        public SlotMachineGame(CCSPlayerController player, int betAmount, List<string> results)
        {
            Player = player;
            BetAmount = betAmount;
            Results = results;
            IsInProgress = false;
            StoppedSlots = new bool[results.Count];
        }
    }
}
