using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using StoreApi;
using MySqlConnector;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace Store_Salary;

public class Store_SalaryConfig : BasePluginConfig
{
    [JsonPropertyName("wages")]
    public Dictionary<string, SalaryConfig> Wages { get; set; } = new()
    {
        { "@css/generic", new SalaryConfig { Salary = 2000, CooldownMinutes = 60 } },
        { "@css/slay", new SalaryConfig { Salary = 3000, CooldownMinutes = 120 } },
        { "@css/vip", new SalaryConfig { Salary = 5000, CooldownMinutes = 1440 } },
        { "#admin", new SalaryConfig { Salary = 6000, CooldownMinutes = 1440 } }
    };

    [JsonPropertyName("salary_command")]
    public List<string> SalaryCommand { get; set; } = ["salary"];

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

    public class SalaryConfig
    {
        [JsonPropertyName("salary")]
        public int Salary { get; set; }

        [JsonPropertyName("cooldown_minutes")]
        public int CooldownMinutes { get; set; }
    }
}

public class Store_Salary : BasePlugin, IPluginConfig<Store_SalaryConfig>
{
    public override string ModuleName => "Store Module [Salary]";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "Nathy";

    public IStoreApi? StoreApi { get; set; }
    public Store_SalaryConfig Config { get; set; } = new();

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        StoreApi = IStoreApi.Capability.Get() ?? throw new Exception("StoreApi could not be located.");
        InitializeDatabase();
        CreateCommands();
    }

    public void OnConfigParsed(Store_SalaryConfig config)
    {
        Config = config;
    }

    private void CreateCommands()
    {
        foreach (var cmd in Config.SalaryCommand)
        {
            AddCommand($"css_{cmd}", "Claims salary", Command_ClaimSalary);
        }
    }

    public void Command_ClaimSalary(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (StoreApi == null) throw new Exception("StoreApi could not be located.");

        using (var connection = new MySqlConnection(GetConnectionString()))
        {
            connection.Open();

            string query = "SELECT LastClaim FROM store_salary WHERE SteamID = @SteamID";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@SteamID", player.SteamID.ToString());

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        DateTime lastClaim = reader.GetDateTime("LastClaim");

                        DateTime nextClaimTime = DateTime.Now;
                        foreach (var wage in Config.Wages)
                        {
                            if (wage.Key.StartsWith('@') && AdminManager.PlayerHasPermissions(player, wage.Key))
                            {
                                nextClaimTime = lastClaim.AddMinutes(wage.Value.CooldownMinutes);
                            }
                            else if (wage.Key.StartsWith('#') && AdminManager.PlayerInGroup(player, wage.Key))
                            {
                                nextClaimTime = lastClaim.AddMinutes(wage.Value.CooldownMinutes);
                            }
                        }

                        TimeSpan timeUntilNextClaim = nextClaimTime - DateTime.Now;

                        if (DateTime.Now < nextClaimTime)
                        {
                            int totalMinutes = (int)timeUntilNextClaim.TotalMinutes;
                            int hours = totalMinutes / 60;
                            int minutes = totalMinutes % 60;

                            player.PrintToChat(Localizer["Prefix"] + Localizer["Salary already claimed", hours, minutes, timeUntilNextClaim.Seconds]);
                            return;
                        }
                    }
                }
            }

            foreach (var wage in Config.Wages)
            {
                if (wage.Key.StartsWith('@') && AdminManager.PlayerHasPermissions(player, wage.Key) ||
                    wage.Key.StartsWith('#') && AdminManager.PlayerInGroup(player, wage.Key))
                {
                    int salary = wage.Value.Salary;
                    StoreApi.GivePlayerCredits(player, salary);

                    string updateQuery = "INSERT INTO store_salary (SteamID, LastClaim) VALUES (@SteamID, @LastClaim) " +
                                        "ON DUPLICATE KEY UPDATE LastClaim = @LastClaim";
                    using (var updateCommand = new MySqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@SteamID", player.SteamID.ToString());
                        updateCommand.Parameters.AddWithValue("@LastClaim", DateTime.Now);
                        updateCommand.ExecuteNonQuery();
                    }

                    player.PrintToChat(Localizer["Prefix"] + Localizer["Salary received", salary]);
                    return;
                }
            }

            player.PrintToChat(Localizer["Prefix"] + Localizer["No salary available"]);
        }
    }

    private void InitializeDatabase()
    {
        using (var connection = new MySqlConnection(GetConnectionString()))
        {
            connection.Open();

            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS store_salary (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    SteamID VARCHAR(255) NOT NULL,
                    LastClaim DATETIME NOT NULL
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
