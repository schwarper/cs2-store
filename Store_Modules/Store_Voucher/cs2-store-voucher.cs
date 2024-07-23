using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using StoreApi;
using MySqlConnector;
using System.Text.Json.Serialization;

public class Store_VoucherConfig : BasePluginConfig
{

    [JsonPropertyName("generate_voucher_admin_only")]
    public bool GenerateVoucherAdminOnly { get; set; } = false;

    [JsonPropertyName("generate_voucher_admin_flag")]
    public string GenerateVoucherAdminFlag { get; set; } = "@css/generic";

    [JsonPropertyName("skip_credit_check_flag_enabled")]
    public bool SkipCreditCheckFlagEnabled { get; set; } = false;

    [JsonPropertyName("skip_credit_check_flag")]
    public string SkipCreditCheckFlag { get; set; } = "@css/slay";

    [JsonPropertyName("generate_voucher_commands")]
    public List<string> GenerateVoucherCommands { get; set; } = ["generate_voucher"];

    [JsonPropertyName("use_voucher_commands")]
    public List<string> UseVoucherCommands { get; set; } = ["use_voucher", "voucher"];

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

public class Store_Voucher : BasePlugin, IPluginConfig<Store_VoucherConfig>
{
    public override string ModuleName => "Store Module [Voucher]";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "Nathy";

    public IStoreApi? StoreApi { get; set; }
    public Store_VoucherConfig Config { get; set; } = new();

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        StoreApi = IStoreApi.Capability.Get() ?? throw new Exception("StoreApi could not be located.");
        InitializeDatabase();
        CreateCommands();
    }

    public void OnConfigParsed(Store_VoucherConfig config)
    {
        Config = config;
    }

    private void CreateCommands()
    {
        foreach (var cmd in Config.GenerateVoucherCommands)
        {
            AddCommand($"css_{cmd}", "Generate vouchers", Command_GenerateVoucher);
        }

        foreach (var cmd in Config.UseVoucherCommands)
        {
            AddCommand($"css_{cmd}", "Use a voucher", Command_UseVoucher);
        }
    }

    [CommandHelper(minArgs: 2, usage: "<quantity> <credits_per_voucher>")]
    public void Command_GenerateVoucher(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (StoreApi == null) throw new Exception("StoreApi could not be located.");

        if (Config.GenerateVoucherAdminOnly && !AdminManager.PlayerHasPermissions(player, Config.GenerateVoucherAdminFlag))
        {
            info.ReplyToCommand(Localizer["Prefix"] + Localizer["No permission to generate voucher"]);
            return;
        }

        bool skipCreditCheck = Config.SkipCreditCheckFlagEnabled && AdminManager.PlayerHasPermissions(player, Config.SkipCreditCheckFlag);

        if (!skipCreditCheck)
        {
            int quantity = int.Parse(info.GetArg(1));
            int creditsPerVoucher = int.Parse(info.GetArg(2));
            int totalCost = quantity * creditsPerVoucher;

            if (StoreApi.GetPlayerCredits(player) < totalCost)
            {
                info.ReplyToCommand(Localizer["Prefix"] + Localizer["Not enough credits"]);
                return;
            }
        }

        int qty = int.Parse(info.GetArg(1));
        int credits = int.Parse(info.GetArg(2));

        GenerateVouchers(player, qty, credits, info, skipCreditCheck);
    }

    [CommandHelper(minArgs: 1, usage: "<voucher_code>")]
    public void Command_UseVoucher(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null) return;

        if (StoreApi == null) throw new Exception("StoreApi could not be located.");

        UseVoucher(player, info.GetArg(1), info);
    }

    private void GenerateVouchers(CCSPlayerController player, int quantity, int creditsPerVoucher, CommandInfo info, bool skipCreditCheck)
    {
        if (StoreApi == null) throw new Exception("StoreApi could not be located.");

        if (!skipCreditCheck)
        {
            int totalCost = quantity * creditsPerVoucher;
            StoreApi.GivePlayerCredits(player, -totalCost);
        }

        using (var connection = new MySqlConnection(GetConnectionString()))
        {
            connection.Open();

            for (int i = 0; i < quantity; i++)
            {
                string voucherCode = GenerateVoucherCode();

                string insertQuery = @"
                    INSERT INTO store_vouchers (SteamID, VoucherCode, Credits)
                    VALUES (@SteamID, @VoucherCode, @Credits)";

                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@SteamID", player.SteamID.ToString());
                    command.Parameters.AddWithValue("@VoucherCode", voucherCode);
                    command.Parameters.AddWithValue("@Credits", creditsPerVoucher);

                    command.ExecuteNonQuery();
                }

                player.PrintToChat(Localizer["Prefix"] + Localizer["Generated voucher", voucherCode, creditsPerVoucher]);
            }
        }
    }

    private void UseVoucher(CCSPlayerController player, string voucherCode, CommandInfo info)
    {
        if (StoreApi == null) throw new Exception("StoreApi could not be located.");
        
        using (var connection = new MySqlConnection(GetConnectionString()))
        {
            connection.Open();

            string query = "SELECT Credits FROM store_vouchers WHERE VoucherCode = @VoucherCode";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@VoucherCode", voucherCode);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int credits = reader.GetInt32("Credits");
                        reader.Close();

                        string deleteQuery = "DELETE FROM store_vouchers WHERE VoucherCode = @VoucherCode";
                        using (var deleteCommand = new MySqlCommand(deleteQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@VoucherCode", voucherCode);
                            deleteCommand.ExecuteNonQuery();
                        }

                        StoreApi.GivePlayerCredits(player, credits);
                        player.PrintToChat(Localizer["Prefix"] + Localizer["Voucher redeemed successfully", credits]);
                    }
                    else
                    {
                        info.ReplyToCommand(Localizer["Prefix"] + Localizer["Invalid or already used"]);
                    }
                }
            }
        }
    }

    private string GenerateVoucherCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        
        var code = new string(Enumerable.Repeat(chars, 16)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        
        return $"{code.Substring(0, 4)}-{code.Substring(4, 4)}-{code.Substring(8, 4)}-{code.Substring(12, 4)}";
    }

    private void InitializeDatabase()
    {
        using (var connection = new MySqlConnection(GetConnectionString()))
        {
            connection.Open();

            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS store_vouchers (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    SteamID VARCHAR(255),
                    VoucherCode VARCHAR(255),
                    Credits INT
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
