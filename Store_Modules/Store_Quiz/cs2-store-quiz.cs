using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using StoreApi;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;

namespace Store_Quiz
{
    public class Store_QuizConfig : BasePluginConfig
    {
        [JsonPropertyName("question_interval_seconds")]
        public int QuestionIntervalSeconds { get; set; } = 30;

        [JsonPropertyName("questions")]
        public List<Question> Questions { get; set; } = [];
    }

    public class Question
    {
        [JsonPropertyName("question")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("answer")]
        public string Answer { get; set; } = string.Empty;

        [JsonPropertyName("credits")]
        public int Credits { get; set; } = 0;
    }

    public class Store_Quiz : BasePlugin, IPluginConfig<Store_QuizConfig>
    {
        public override string ModuleName { get; } = "Store Module [Quiz]";
        public override string ModuleVersion { get; } = "0.0.1";
        public override string ModuleAuthor => "Nathy";

        public Store_QuizConfig Config { get; set; } = null!;
        private Timer? quizTimer;
        private int currentQuestionIndex = 0;
        private bool questionAnswered = false;
        private IStoreApi? storeApi;
        private readonly object timerLock = new();

        public void OnConfigParsed(Store_QuizConfig config)
        {
            Config = config;
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            storeApi = IStoreApi.Capability.Get();

            if (Config.Questions.Count == 0)
            {
                Console.WriteLine("No questions available in the configuration.");
                return;
            }

            quizTimer = new Timer(AskQuestion, null, Timeout.Infinite, Timeout.Infinite);
            StartQuizTimer();
            
            AddCommandListener("say", OnPlayerChatAll);
            AddCommandListener("say_team", OnPlayerChatTeam);
        }

        private void StartQuizTimer()
        {
            lock (timerLock)
            {
                quizTimer?.Change(0, Timeout.Infinite);
            }
        }

        private void AskQuestion(object? state)
        {
            lock (timerLock)
            {
                if (Config.Questions.Count == 0)
                {
                    Console.WriteLine("No questions available.");
                    return;
                }

                if (!questionAnswered)
                {
                    MoveToNextQuestion();
                }

                questionAnswered = false;
                var question = Config.Questions[currentQuestionIndex];

                Server.NextFrame(() =>
                {
                    Server.PrintToChatAll(Localizer["Quiz.Question", question.QuestionText]);
                });

                quizTimer?.Change(Config.QuestionIntervalSeconds * 1000, Timeout.Infinite);
            }
        }

        private HookResult OnPlayerChatAll(CCSPlayerController? player, CommandInfo message)
        {
            if (player == null)
            {
                return HookResult.Handled;
            }

            if (!questionAnswered)
            {
                var answer = message.GetArg(1);
                var currentQuestion = Config.Questions[currentQuestionIndex];

                if (answer.Equals(currentQuestion.Answer, StringComparison.OrdinalIgnoreCase))
                {
                    questionAnswered = true;
                    Server.PrintToChatAll(Localizer["Quiz.AnsweredCorrectly", player.PlayerName]);

                    if (storeApi != null && currentQuestion.Credits > 0)
                    {
                        storeApi.GivePlayerCredits(player, currentQuestion.Credits);
                        Server.PrintToChatAll(Localizer["Quiz.Awarded", player.PlayerName, currentQuestion.Credits]);
                    }

                    MoveToNextQuestion();
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerChatTeam(CCSPlayerController? player, CommandInfo message)
        {
            if (player == null)
            {
                return HookResult.Handled;
            }

            if (!questionAnswered)
            {
                var answer = message.GetArg(1);
                var currentQuestion = Config.Questions[currentQuestionIndex];

                if (answer.Equals(currentQuestion.Answer, StringComparison.OrdinalIgnoreCase))
                {
                    questionAnswered = true;
                    Server.PrintToChatAll(Localizer["Quiz.AnsweredCorrectly", player.PlayerName]);

                    if (storeApi != null && currentQuestion.Credits > 0)
                    {
                        storeApi.GivePlayerCredits(player, currentQuestion.Credits);
                        Server.PrintToChatAll(Localizer["Quiz.Awarded", player.PlayerName, currentQuestion.Credits]);
                    }

                    MoveToNextQuestion();
                }
            }
            else
            {
                player.PrintToChat(Localizer["Quiz.AlreadyAnswered"]);
            }
            return HookResult.Continue;
        }

        private void MoveToNextQuestion()
        {
            currentQuestionIndex = (currentQuestionIndex + 1) % Config.Questions.Count;
        }
    }
}
