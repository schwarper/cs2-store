using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Dapper;
using MySqlConnector;
using StoreApi;
using System.Text.Json.Serialization;

namespace Store_TopList
{
    public class Store_TopListConfig : BasePluginConfig
    {
        [JsonPropertyName("top_players_limit")]
        public int TopPlayersLimit { get; set; } = 10;

        [JsonPropertyName("commands")]
        public List<string> Commands { get; set; } = [];
    }

    public class Store_TopList : BasePlugin, IPluginConfig<Store_TopListConfig>
    {
        public override string ModuleName { get; } = "Store Module [TopList]";
        public override string ModuleVersion { get; } = "0.0.2";
        public override string ModuleAuthor => "Nathy";

        private IStoreApi? storeApi;

        public Store_TopListConfig Config { get; set; } = null!;

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            storeApi = IStoreApi.Capability.Get();

            if (storeApi == null)
            {
                return;
            }

            CreateCommands();
        }

        public void OnConfigParsed(Store_TopListConfig config)
        {
            Config = config;
        }

        private void CreateCommands()
        {
            foreach (string cmd in Config.Commands)
            {
                AddCommand($"css_{cmd}", "Shows top list by credits", OnCommand);
            }
        }

        public void OnCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null)
            {
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    List<TopPlayer> topPlayers = await GetTopPlayersByCreditsAsync();

                    Server.NextFrame(() =>
                    {
                        if (topPlayers == null)
                        {
                            player.PrintToChat("Failed to retrieve top players.");
                            return;
                        }

                        player.PrintToChat(Localizer["topcredits.title"]);

                        int rank = 1;
                        foreach (TopPlayer topPlayer in topPlayers)
                        {
                            player.PrintToChat(Localizer["topcredits.players", rank, topPlayer.PlayerName, topPlayer.Credits]);
                            rank++;
                        }
                        player.PrintToChat(Localizer["topcredits.bottom"]);
                    });
                }
                catch (Exception ex)
                {
                    player.PrintToChat("An error occurred while retrieving the top players.");
                    Console.WriteLine($"An error occurred while retrieving the top players. Exception: {ex.Message}");
                }
            });
        }

        private async Task<List<TopPlayer>> GetTopPlayersByCreditsAsync()
        {
            string connectionString = GetDatabaseString();

            using MySqlConnection connection = new(connectionString);
            await connection.OpenAsync();

            string query = $@"
                SELECT PlayerName, Credits
                FROM store_players
                ORDER BY Credits DESC
                LIMIT {Config.TopPlayersLimit};";

            return (await connection.QueryAsync<TopPlayer>(query)).AsList();
        }

        public string GetDatabaseString()
        {
            if (storeApi == null)
            {
                throw new InvalidOperationException("Store API is not available.");
            }

            return storeApi.GetDatabaseString();
        }
    }

    public class TopPlayer
    {
        public string PlayerName { get; set; } = string.Empty;
        public int Credits { get; set; }
    }
}
