using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using StoreApi;
using MySqlConnector;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace Store_DailyRewards;

public class Store_DailyRewardsConfig : BasePluginConfig
{
    [JsonPropertyName("daily_rewards")]
    public Dictionary<int, int> DailyRewards { get; set; } = new() { { 1, 1000 }, { 2, 2000 }, { 3, 3000 }, { 4, 4000 }, { 5, 5000 }, { 6, 6000 }, { 7, 7000 } };

    [JsonPropertyName("daily_commands")]
    public List<string> ClaimDailyCommands { get; set; } = ["daily", "claimdaily"];
    
    [JsonPropertyName("daily_message_type")]
    public int DailyMessageType { get; set; } = 1;

    [JsonPropertyName("database_host")]
    public string DatabaseHost { get; set; } = "localhost";

    [JsonPropertyName("database_port")]
    public int DatabasePort { get; set; } = 3306;

    [JsonPropertyName("database_name")]
    public string DatabaseName { get; set; } = "name";

    [JsonPropertyName("database_user")]
    public string DatabaseUser { get; set; } = "root";

    [JsonPropertyName("database_password")]
    public string DatabasePassword { get; set; } = "password";
}

public class Store_DailyRewards : BasePlugin, IPluginConfig<Store_DailyRewardsConfig>
{
    public override string ModuleName => "Store Module [Daily Rewards]";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "Nathy";

    public IStoreApi? StoreApi { get; set; }
    public Store_DailyRewardsConfig Config { get; set; } = new();

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        StoreApi = IStoreApi.Capability.Get() ?? throw new Exception("StoreApi could not be located.");
        InitializeDatabase();
        CreateCommands();
    }

    public void OnConfigParsed(Store_DailyRewardsConfig config)
    {
        Config = config;
    }

    private void CreateCommands()
    {
        foreach (var cmd in Config.ClaimDailyCommands)
        {
            AddCommand($"css_{cmd}", "Claims daily reward", Command_ClaimReward);
        }
    }

    public void Command_ClaimReward(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (StoreApi == null) throw new Exception("StoreApi could not be located.");

        using (var connection = new MySqlConnection(GetConnectionString()))
        {
            connection.Open();

            string query = "SELECT LastLogin, ConsecutiveDays FROM store_daily WHERE SteamID = @SteamID";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@SteamID", player.SteamID.ToString());

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        DateTime lastLogin = reader.GetDateTime("LastLogin");
                        int consecutiveDays = reader.GetInt32("ConsecutiveDays");
                        DateTime today = DateTime.Now.Date;

                        if (lastLogin.Date == today)
                        {
                            DateTime nextClaimTime = lastLogin.AddDays(1);
                            TimeSpan timeUntilNextClaim = nextClaimTime - DateTime.Now;

                            player.PrintToChat(Localizer["Prefix"] + Localizer["Already claimed todays reward", timeUntilNextClaim.Hours, timeUntilNextClaim.Minutes]);
                        }
                        else
                        {
                            if (lastLogin.Date == today.AddDays(-1))
                            {
                                consecutiveDays++;
                            }
                            else
                            {
                                consecutiveDays = 1;
                            }

                            int reward = Config.DailyRewards.ContainsKey(consecutiveDays) ? Config.DailyRewards[consecutiveDays] : Config.DailyRewards[1];
                            StoreApi.GivePlayerCredits(player, reward);

                            reader.Close();

                            string updateQuery = "UPDATE store_daily SET LastLogin = @LastLogin, ConsecutiveDays = @ConsecutiveDays WHERE SteamID = @SteamID";
                            using (var updateCommand = new MySqlCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@LastLogin", today);
                                updateCommand.Parameters.AddWithValue("@ConsecutiveDays", consecutiveDays);
                                updateCommand.Parameters.AddWithValue("@SteamID", player.SteamID.ToString());
                                updateCommand.ExecuteNonQuery();
                            }

                            if (Config.DailyMessageType == 1)
                            {
                                player.PrintToChat(Localizer["Prefix"] + Localizer["You received your daily reward", reward, consecutiveDays]);
                            }
                            else if (Config.DailyMessageType == 2)
                            {
                                PrintDailyRewards(player, consecutiveDays);
                            }
                        }
                    }
                    else
                    {
                        reader.Close();

                        string insertQuery = "INSERT INTO store_daily (SteamID, LastLogin, ConsecutiveDays) VALUES (@SteamID, @LastLogin, @ConsecutiveDays)";
                        using (var insertCommand = new MySqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@SteamID", player.SteamID.ToString());
                            insertCommand.Parameters.AddWithValue("@LastLogin", DateTime.Now.Date);
                            insertCommand.Parameters.AddWithValue("@ConsecutiveDays", 1);
                            insertCommand.ExecuteNonQuery();
                        }

                        int reward = Config.DailyRewards[1];
                        StoreApi.GivePlayerCredits(player, reward);

                        if (Config.DailyMessageType == 1)
                        {
                            player.PrintToChat(Localizer["Prefix"] + Localizer["You received your first daily reward", reward]);
                        }
                        else if (Config.DailyMessageType == 2)
                        {
                            PrintDailyRewards(player, 1);
                        }
                    }
                }
            }
        }
    }

    private void PrintDailyRewards(CCSPlayerController player, int consecutiveDays)
    {
        int startDay = ((consecutiveDays - 1) / 7) * 7 + 1;
        int endDay = startDay + 6;

        player.PrintToChat(Localizer["Detailed daily reward first line"]);

        for (int day = startDay; day <= endDay; day++)
        {
            string messageKey = (day <= consecutiveDays) ? "Detailed daily reward claimed" : "Detailed daily reward not claimed";
            int reward = Config.DailyRewards.ContainsKey(day) ? Config.DailyRewards[day] : 0;
            player.PrintToChat(Localizer["Prefix"] + Localizer[messageKey, day, reward]);
        }

        player.PrintToChat(Localizer["Detailed daily reward last line"]);
    }

    private void InitializeDatabase()
    {
        using (var connection = new MySqlConnection(GetConnectionString()))
        {
            connection.Open();

            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS store_daily (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    SteamID VARCHAR(255) NOT NULL,
                    LastLogin DATETIME NOT NULL,
                    ConsecutiveDays INT NOT NULL
                )";

            using (var command = new MySqlCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    private string GetConnectionString()
    {
        return $"Server={Config.DatabaseHost};Port={Config.DatabasePort};Database={Config.DatabaseName};Uid={Config.DatabaseUser};Pwd={Config.DatabasePassword};";
    }
}
