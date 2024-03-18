using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Dapper;
using MySqlConnector;
using static Store.Store;

namespace Store;

internal static class Database
{
    internal static string GlobalDatabaseConnectionString { get; set; } = string.Empty;

    internal static MySqlConnection Connect()
    {
        MySqlConnection connection = new(GlobalDatabaseConnectionString);
        connection.Open();
        return connection;
    }

    internal static async Task ExecuteAsync(string query, object? parameters)
    {
        using MySqlConnection connection = Connect();
        await connection.ExecuteAsync(query, parameters);
    }

    internal static void CreateDatabase(StoreConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Database["host"]) ||
            string.IsNullOrWhiteSpace(config.Database["name"]) ||
            string.IsNullOrWhiteSpace(config.Database["user"]))
        {
            throw new Exception("[cs2-market] Database credentials in config must not be empty!");
        }

        MySqlConnectionStringBuilder builder = new()
        {
            Server = config.Database["host"],
            Database = config.Database["name"],
            UserID = config.Database["user"],
            Password = config.Database["password"],
            Port = uint.Parse(config.Database["port"]),
            AllowZeroDateTime = true
        };

        GlobalDatabaseConnectionString = builder.ConnectionString;

        Connect();

        _ = Task.Run(async () =>
        {
            using MySqlConnection connection = Connect();
            using MySqlTransaction transaction = await connection.BeginTransactionAsync();

            try
            {
                await connection.QueryAsync(@"
                    CREATE TABLE IF NOT EXISTS store_players (
                        id INT NOT NULL AUTO_INCREMENT,
                        SteamID BIGINT UNSIGNED NOT NULL,
                        PlayerName varchar(128) NOT NULL,
                        Credits INT NOT NULL,
                        DateOfJoin TIMESTAMP NOT NULL,
                        DateOfLastJoin TIMESTAMP NOT NULL,
                        PRIMARY KEY (id),
                        UNIQUE KEY id (id),
                        UNIQUE KEY SteamID (SteamID)
				);", transaction: transaction);

                await connection.QueryAsync(@"
                    CREATE TABLE IF NOT EXISTS store_items (
                        id INT NOT NULL AUTO_INCREMENT,
                        SteamID BIGINT UNSIGNED NOT NULL,
                        Price INT UNSIGNED NOT NULL,
                        Type varchar(16) NOT NULL,
                        UniqueId varchar(256) NOT NULL,
                        Slot int,
                        Color varchar(11),
                        DateOfPurchase TIMESTAMP NOT NULL,
                        PRIMARY KEY (id)
			    );", transaction: transaction);

                await connection.QueryAsync(@"
                    CREATE TABLE IF NOT EXISTS store_equipments (
                        id INT NOT NULL AUTO_INCREMENT,
                        SteamID BIGINT UNSIGNED NOT NULL,
                        Type varchar(16) NOT NULL,
                        UniqueId varchar(256) NOT NULL,
                        Slot INT,
                        Color varchar(11),
                        PRIMARY KEY (id)
			    );", transaction: transaction);

                await transaction.CommitAsync();

                IEnumerable<Store_Player> storePlayers = await connection.QueryAsync<Store_Player>("SELECT * FROM store_players;");

                foreach (Store_Player player in storePlayers)
                {
                    Instance.GlobalStorePlayers.Add(player);
                }

                IEnumerable<Store_PlayerItem> storeItems = await connection.QueryAsync<Store_PlayerItem>("SELECT * FROM store_items;");

                foreach (Store_PlayerItem item in storeItems)
                {
                    if (Item.IsInJson(item.Type, item.UniqueId))
                    {
                        Instance.GlobalStorePlayerItems.Add(item);
                    }
                }

                IEnumerable<Store_PlayerItem> storeEquipments = await connection.QueryAsync<Store_PlayerItem>("SELECT * FROM store_equipments;");

                foreach (Store_PlayerItem item in storeItems)
                {
                    if (Item.IsInJson(item.Type, item.UniqueId))
                    {
                        Instance.GlobalStorePlayerEquipments.Add(item);
                    }
                }
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    internal static async Task LoadPlayer(CCSPlayerController player)
    {
        try
        {
            using MySqlConnection connection = Connect();

            dynamic result = await connection.QueryFirstOrDefaultAsync(@"SELECT * FROM store_players WHERE SteamID = @SteamID",
            new
            {
                player.SteamID
            });

            Server.NextFrame(async () =>
            {
                if (result == null)
                {
                    Instance.GlobalStorePlayers.Add(new Store_Player
                    {
                        SteamID = player.SteamID,
                        PlayerName = player.PlayerName,
                        Credits = Instance.Config.Credits["start"],
                        DateOfJoin = DateTime.Now,
                        DateOfLastJoin = DateTime.Now
                    });

                    await InsertNewPlayer(player);
                }
            });
        }
        catch (MySqlException ex)
        {
            throw new Exception($"Database error: {ex}");
        }
    }

    internal static async Task InsertNewPlayer(CCSPlayerController player)
    {
        await ExecuteAsync(@"
                INSERT INTO store_players (
                    SteamID, PlayerName, Credits, DateOfJoin, DateOfLastJoin
                ) VALUES (
                    @SteamID, @PlayerName, @Credits, @DateOfJoin, @DateOfLastJoin
                );"
            ,
            new
            {
                player.SteamID,
                player.PlayerName,
                Credits = Instance.Config.Credits["start"],
                DateOfJoin = DateTime.Now,
                DateOfLastJoin = DateTime.Now
            });
    }

    internal static async Task SavePlayer(CCSPlayerController player, string playername)
    {
        await ExecuteAsync(@$"
                UPDATE
                    store_players
                SET
                    PlayerName = @PlayerName, Credits = @Credits, DateOfJoin = @DateOfJoin, DateOfLastJoin = @DateOfLastJoin
                WHERE
                    SteamID = @SteamID;
            ",
            new
            {
                playername,
                Credits = Credits.Get(player),
                player.SteamID,
                DateOfJoin = DateTime.Now,
                DateOfLastJoin = DateTime.Now
            });
    }

    internal static async Task SavePlayerItem(CCSPlayerController player, Store_PlayerItem item)
    {
        await ExecuteAsync(@"
                INSERT INTO store_items (
                    SteamID, Price, Type, UniqueId, Slot, Color, DateOfPurchase
                ) VALUES (
                    @SteamID, @Price, @Type, @UniqueId, @Slot, @Color, @DateOfPurchase
                );"
            ,
            new
            {
                player.SteamID,
                item.Price,
                item.Type,
                item.UniqueId,
                item.Slot,
                item.Color,
                DateOfPurchase = DateTime.Now
            });
    }
    internal static async Task RemovePlayerItem(CCSPlayerController player, Store_PlayerItem item)
    {
        await ExecuteAsync(@"
                DELETE
                FROM
                    store_items
                WHERE
                    SteamID = @SteamID AND UniqueId = @UniqueId;
            "
            ,
            new
            {
                player.SteamID,
                item.UniqueId
            });
    }
    internal static async Task SavePlayerEquipment(CCSPlayerController player, Store_PlayerItem item)
    {
        await ExecuteAsync(@$"
                REPLACE INTO store_equipments (
                    SteamID, Type, UniqueId, Slot, Color
                ) VALUES (
                    @SteamID, @Type, @UniqueId, @Slot, @Color
                );
            "
            ,
            new
            {
                player.SteamID,
                item.Type,
                item.UniqueId,
                item.Slot,
                item.Color
            });
    }
    internal static async Task RemovePlayerEquipment(CCSPlayerController player, string UniqueId)
    {
        await ExecuteAsync(@"
                DELETE FROM store_equipments WHERE SteamID = @SteamID AND UniqueId = @UniqueId;
            "
            ,
            new
            {
                player.SteamID,
                UniqueId
            });
    }

    internal static void ResetDatabase()
    {
        using MySqlConnection connection = Connect();

        connection.Query(@"DROP TABLE store_players");
        connection.Query(@"DROP TABLE store_items");
        connection.Query(@"DROP TABLE store_equipment");

        Server.ExecuteCommand("_restart");
    }
}