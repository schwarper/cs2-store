using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Dapper;
using MySqlConnector;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Database
{
    public static string GlobalDatabaseConnectionString { get; set; } = string.Empty;

    public static MySqlConnection Connect()
    {
        MySqlConnection connection = new(GlobalDatabaseConnectionString);
        connection.Open();
        return connection;
    }

    public static void Execute(string query, object? parameters)
    {
        using MySqlConnection connection = Connect();
        connection.Execute(query, parameters);
    }

    public static void CreateDatabase(StoreConfig config)
    {
        MySqlConnectionStringBuilder builder = new()
        {
            Server = config.Database["host"],
            Database = config.Database["name"],
            UserID = config.Database["user"],
            Password = config.Database["password"],
            Port = uint.Parse(config.Database["port"]),
            Pooling = true,
            MinimumPoolSize = 0,
            MaximumPoolSize = 640,
            ConnectionIdleTimeout = 30,
            AllowZeroDateTime = true
        };

        GlobalDatabaseConnectionString = builder.ConnectionString;

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
                        PlayerName VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
                        Credits INT NOT NULL,
                        DateOfJoin DATETIME NOT NULL,
                        DateOfLastJoin DATETIME NOT NULL,
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
                        DateOfPurchase DATETIME NOT NULL,
                        DateOfExpiration DATETIME NOT NULL,
                        PRIMARY KEY (id)
			    );", transaction: transaction);

                await connection.QueryAsync(@"
                    CREATE TABLE IF NOT EXISTS store_equipments (
                        id INT NOT NULL AUTO_INCREMENT,
                        SteamID BIGINT UNSIGNED NOT NULL,
                        Type varchar(16) NOT NULL,
                        UniqueId varchar(256) NOT NULL,
                        Slot INT,
                        PRIMARY KEY (id)
			    );", transaction: transaction);

                IEnumerable<Store_Player> store_players = await connection.QueryAsync<Store_Player>("SELECT * FROM store_players;", transaction: transaction);

                foreach (Store_Player player in store_players)
                {
                    Instance.GlobalStorePlayers.Add(player);
                }

                IEnumerable<Store_Item> store_items = await connection.QueryAsync<Store_Item>("SELECT * FROM store_items;", transaction: transaction);

                foreach (Store_Item item in store_items)
                {
                    if (Item.IsInJson(item.Type, item.UniqueId))
                    {
                        Instance.GlobalStorePlayerItems.Add(item);
                    }
                }

                IEnumerable<Store_Equipment> store_equipments = await connection.QueryAsync<Store_Equipment>("SELECT * FROM store_equipments;", transaction: transaction);

                foreach (Store_Equipment equipment in store_equipments)
                {
                    if (Item.IsInJson(equipment.Type, equipment.UniqueId))
                    {
                        Instance.GlobalStorePlayerEquipments.Add(equipment);
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public static async Task LoadPlayer(CCSPlayerController player)
    {
        using MySqlConnection connection = Connect();

        dynamic result = await connection.QueryFirstOrDefaultAsync(@"SELECT * FROM store_players WHERE SteamID = @SteamID",
        new
        {
            player.SteamID
        });

        Server.NextFrame(() =>
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

                InsertNewPlayer(player);
            }
        });
    }

    public static void InsertNewPlayer(CCSPlayerController player)
    {
        Execute(@"
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

    public static void SavePlayer(CCSPlayerController player)
    {
        Execute(@"
                UPDATE
                    store_players
                SET
                    PlayerName = @PlayerName, Credits = @Credits, DateOfJoin = @DateOfJoin, DateOfLastJoin = @DateOfLastJoin
                WHERE
                    SteamID = @SteamID;
            ",
            new
            {
                player.PlayerName,
                Credits = Credits.Get(player),
                DateOfJoin = DateTime.Now,
                DateOfLastJoin = DateTime.Now,
                SteamId = player.SteamID,
            });
    }

    public static void SavePlayerItem(CCSPlayerController player, Store_Item item)
    {
        Execute(@"
            INSERT INTO store_items (
                SteamID, Price, Type, UniqueId, DateOfPurchase, DateOfExpiration
            ) VALUES (
                @SteamID, @Price, @Type, @UniqueId, @DateOfPurchase, @DateOfExpiration
            );
           "
            ,
            new
            {
                player.SteamID,
                item.Price,
                item.Type,
                item.UniqueId,
                item.DateOfPurchase,
                item.DateOfExpiration
            });
    }
    public static void RemovePlayerItem(CCSPlayerController player, Store_Item item)
    {
        Execute(@"
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
    public static void SavePlayerEquipment(CCSPlayerController player, Store_Equipment item)
    {
        Execute(@"
                INSERT INTO store_equipments (
                    SteamID, Type, UniqueId, Slot
                ) VALUES (
                    @SteamID, @Type, @UniqueId, @Slot
                );
            "
            ,
            new
            {
                player.SteamID,
                item.Type,
                item.UniqueId,
                item.Slot
            });
    }
    public static void RemovePlayerEquipment(CCSPlayerController player, string UniqueId)
    {
        Execute(@"
                DELETE FROM store_equipments WHERE SteamID = @SteamID AND UniqueId = @UniqueId;
            "
            ,
            new
            {
                player.SteamID,
                UniqueId
            });
    }

    public static void ResetPlayer(CCSPlayerController player)
    {
        Execute(@"
            DELETE FROM store_items WHERE SteamID = @SteamID; 
            DELETE FROM store_equipments WHERE SteamID = @SteamID
        "
        ,
        new
        {
            player.SteamID
        });
    }

    public static void ResetDatabase()
    {
        using MySqlConnection connection = Connect();

        connection.Query(@"DROP TABLE store_players");
        connection.Query(@"DROP TABLE store_items");
        connection.Query(@"DROP TABLE store_equipment");

        Server.ExecuteCommand("_restart");
    }
}