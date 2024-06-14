using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Dapper;
using MySqlConnector;
using System.Text.Json.Serialization;
using StoreApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Store_TopList
{
    public class Store_TopListConfig : BasePluginConfig
    {
        [JsonPropertyName("top_players_limit")]
        public int TopPlayersLimit { get; set; } = 10;
    }

    public class Store_TopList : BasePlugin, IPluginConfig<Store_TopListConfig>
    {
        public override string ModuleName { get; } = "Store Module [TopList]";
        public override string ModuleVersion { get; } = "0.0.1";

        private IStoreApi? storeApi;

        public Store_TopListConfig Config { get; set; } = null!;

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            storeApi = IStoreApi.Capability.Get();

            if (storeApi == null)
            {
                return;
            }
        }

        public void OnConfigParsed(Store_TopListConfig config)
        {
            Config = config;
        }

        [ConsoleCommand("css_topcredits", "Get topX players by credits")]
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
                    var topPlayers = await GetTopPlayersByCreditsAsync();

                    Server.NextFrame(() =>
                    {
                        if (topPlayers == null)
                        {
                            player.PrintToChat("Failed to retrieve top players.");
                            return;
                        }

                        player.PrintToChat(Localizer["topcredits.title"]);

                        int rank = 1;
                        foreach (var topPlayer in topPlayers)
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

            var query = $@"
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
        public string PlayerName { get; set; }
        public int Credits { get; set; }
    }
}
