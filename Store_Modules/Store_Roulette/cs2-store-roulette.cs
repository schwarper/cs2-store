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

namespace Store_Roulette;

public class Store_RouletteConfig : BasePluginConfig
{
    [JsonPropertyName("min_roulette")]
    public int MinRoulette { get; set; } = 0;

    [JsonPropertyName("max_roulette")]
    public int MaxRoulette { get; set; } = 3000;

    [JsonPropertyName("roulette_commands")]
    public string[] RouletteCommands { get; set; } = ["css_roulette"];

    [JsonPropertyName("red")]
    public Dictionary<string, int> Red { get; set; } = new Dictionary<string, int>
    {
        { "multiplier", 2 },
        { "probability", 49 }
    };

    [JsonPropertyName("blue")]
    public Dictionary<string, int> Blue { get; set; } = new Dictionary<string, int>
    {
        { "multiplier", 2 },
        { "probability", 49 }
    };

    [JsonPropertyName("green")]
    public Dictionary<string, int> Green { get; set; } = new Dictionary<string, int>
    {
        { "multiplier", 14 },
        { "probability", 2 }
    };
}

public class Store_Roulette : BasePlugin, IPluginConfig<Store_RouletteConfig>
{
    public override string ModuleName => "[Store Module] Roulette";
    public override string ModuleVersion => "0.0.2";
    public override string ModuleAuthor => "schwarper";

    public IStoreApi? StoreApi { get; set; }
    public Dictionary<Color, Dictionary<CCSPlayerController, int>> GlobalRoulette { get; set; } = [];
    public Random Random { get; set; } = new();
    public Store_RouletteConfig Config { get; set; } = new Store_RouletteConfig();
    public List<Color> Colors = [Color.Red, Color.Blue, Color.Green];

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        StoreApi = IStoreApi.Capability.Get() ?? throw new Exception("StoreApi could not be located.");

        foreach (string command in Config.RouletteCommands)
        {
            AddCommand(command, "Roulette", Command_Roulette);
        }

        foreach (Color color in Colors)
        {
            GlobalRoulette.Add(color, []);
        }
    }

    public void OnConfigParsed(Store_RouletteConfig config)
    {
        config.MinRoulette = Math.Max(0, config.MinRoulette);
        config.MaxRoulette = Math.Max(config.MinRoulette + 1, config.MaxRoulette);

        static void UpdateMultiplierAndProbability(Dictionary<string, int> color)
        {
            color["multiplier"] = Math.Max(color["multiplier"], 1);
            color["probability"] = Math.Max(color["probability"], 1);
        }

        UpdateMultiplierAndProbability(config.Red);
        UpdateMultiplierAndProbability(config.Blue);
        UpdateMultiplierAndProbability(config.Green);

        Config = config;
    }

    public void Command_Roulette(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        if (StoreApi == null)
        {
            throw new Exception("StoreApi could not be located.");
        }

        if (Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!.WarmupPeriod)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Cannot join during warmup"]);
            return;
        }

        if (GlobalRoulette.Values.Any(dict => dict.ContainsKey(player)))
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

        if (credits < Config.MinRoulette)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Min roulette", Config.MinRoulette]);
            return;
        }

        if (credits > Config.MaxRoulette)
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["Max roulette", Config.MaxRoulette]);
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

            AddRouletteOption(menu, info, credits, Color.Red, Config.Red["multiplier"]);
            AddRouletteOption(menu, info, credits, Color.Blue, Config.Blue["multiplier"]);
            AddRouletteOption(menu, info, credits, Color.Green, Config.Green["multiplier"]);

            MenuManager.OpenCenterHtmlMenu(this, player, menu);
        }
    }

    public void AddRouletteOption(CenterHtmlMenu menu, CommandInfo info, int credits, Color color, int multiplier)
    {
        StringBuilder builder = new();
        builder.AppendFormat(Localizer["menu_options", Localizer[color.Name], multiplier]);

        menu.AddMenuOption(builder.ToString(), (player, option) =>
        {
            JoinRoulette(player, info, credits, color);
        });
    }

    public void JoinRoulette(CCSPlayerController player, CommandInfo info, int credits, Color color)
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
        GlobalRoulette[color].Add(player, credits);

        char chatcolor = FindChatColor(color);
        PrintToChatAll("Join roulette", player.PlayerName, credits, chatcolor, Localizer[color.Name]);
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (StoreApi == null)
        {
            throw new Exception("StoreApi could not be located.");
        }

        Pay();

        foreach (Color color in Colors)
        {
            GlobalRoulette[color].Clear();
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        int totalProbability = Config.Red["probability"] + Config.Blue["probability"] + Config.Green["probability"];

        int randomNumber = Random.Next(1, totalProbability + 1);

        if (randomNumber <= Config.Red["probability"])
        {
            GiveCreditsToWinner(Color.Red);
        }
        else if (randomNumber <= Config.Red["probability"] + Config.Blue["probability"])
        {
            GiveCreditsToWinner(Color.Blue);
        }
        else
        {
            GiveCreditsToWinner(Color.Green);
        }

        return HookResult.Continue;
    }

    public void GiveCreditsToWinner(Color color)
    {
        if (StoreApi == null)
        {
            throw new Exception("StoreApi could not be located.");
        }

        char chatcolor = FindChatColor(color);

        PrintToChatAll("Winner roulette", chatcolor, Localizer[color.Name]);
        Pay(color);

        foreach (Color colorx in Colors)
        {
            GlobalRoulette[colorx].Clear();
        }
    }

    public void Pay(Color color)
    {
        int multiplier = FindMultiplier(color);

        foreach (KeyValuePair<CCSPlayerController, int> kv in GlobalRoulette[color])
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
        foreach (KeyValuePair<Color, Dictionary<CCSPlayerController, int>> kv in GlobalRoulette)
        {
            Color color = kv.Key;
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

    public static char FindChatColor(Color color)
    {
        KnownColor knowncolor = color.ToKnownColor();

        return knowncolor switch
        {
            KnownColor.Red => ChatColors.Red,
            KnownColor.Blue => ChatColors.Blue,
            KnownColor.Green => ChatColors.Green,
            _ => ChatColors.White
        };
    }

    public int FindMultiplier(Color color)
    {
        KnownColor knowncolor = color.ToKnownColor();

        return knowncolor switch
        {
            KnownColor.Red => Config.Red["multiplier"],
            KnownColor.Blue => Config.Blue["multiplier"],
            KnownColor.Green => Config.Green["multiplier"],
            _ => ChatColors.White
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
