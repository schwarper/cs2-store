using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Database
{
    public static string GlobalDatabaseConnectionString { get; set; } = string.Empty;

    public static async Task<MySqlConnection> ConnectAsync()
    {
        MySqlConnection connection = new(GlobalDatabaseConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public static void ExecuteAsync(string query, object? parameters)
    {
        Task.Run(async () =>
        {
            using MySqlConnection connection = await ConnectAsync();
            await connection.ExecuteAsync(query, parameters);
        });
    }

    public static async Task CreateDatabaseAsync(Config_DatabaseConnection config)
    {
        MySqlConnectionStringBuilder builder = new()
        {
            Server = config.Host,
            Database = config.Name,
            UserID = config.User,
            Password = config.Pass,
            Port = config.Port,
            Pooling = true,
            MinimumPoolSize = 0,
            MaximumPoolSize = 640,
            ConnectionIdleTimeout = 30,
            AllowZeroDateTime = true
        };

        GlobalDatabaseConnectionString = builder.ConnectionString;

        using MySqlConnection connection = await ConnectAsync();
        using MySqlTransaction transaction = await connection.BeginTransactionAsync();

        try
        {
            string store_players = config.StorePlayersName;
            string store_items = config.StoreItemsName;
            string store_equipments = config.StoreEquipments;

            await connection.ExecuteAsync($@"
                CREATE TABLE IF NOT EXISTS {store_players} (
                    id INT NOT NULL AUTO_INCREMENT,
                    SteamID BIGINT UNSIGNED NOT NULL,
                    PlayerName VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
                    Credits INT NOT NULL,
                    DateOfJoin DATETIME NOT NULL,
                    DateOfLastJoin DATETIME NOT NULL,
                    Vip BOOLEAN NOT NULL,
                    PRIMARY KEY (id),
                    UNIQUE KEY id (id),
                    UNIQUE KEY SteamID (SteamID)
                );", transaction: transaction);

            await connection.ExecuteAsync($@"
                CREATE TABLE IF NOT EXISTS {store_items} (
                    id INT NOT NULL AUTO_INCREMENT,
                    SteamID BIGINT UNSIGNED NOT NULL,
                    Price INT UNSIGNED NOT NULL,
                    Type varchar(16) NOT NULL,
                    UniqueId varchar(256) NOT NULL,
                    DateOfPurchase DATETIME NOT NULL,
                    DateOfExpiration DATETIME NOT NULL,
                    PRIMARY KEY (id)
                );", transaction: transaction);

            await connection.ExecuteAsync($@"
                CREATE TABLE IF NOT EXISTS {store_equipments} (
                    id INT NOT NULL AUTO_INCREMENT,
                    SteamID BIGINT UNSIGNED NOT NULL,
                    Type varchar(16) NOT NULL,
                    UniqueId varchar(256) NOT NULL,
                    Slot INT,
                    PRIMARY KEY (id)
                );", transaction: transaction);

            await connection.ExecuteAsync($@"
                DELETE FROM {store_equipments}
                    WHERE NOT EXISTS (
                        SELECT 1 FROM {store_items}
                        WHERE {store_items}.Type = {store_equipments}.Type
                        AND {store_items}.UniqueId = {store_equipments}.UniqueId
                        AND {store_items}.SteamID = {store_equipments}.SteamID
                    )
                    AND EXISTS (
                        SELECT 1 
                        FROM {Config.DatabaseConnection.StorePlayersName}
                        WHERE {Config.DatabaseConnection.StorePlayersName}.SteamID = {store_equipments}.SteamID
                        AND {Config.DatabaseConnection.StorePlayersName}.Vip = FALSE
                    )
                ;", transaction: transaction);

            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static void UpdateVip(CCSPlayerController player)
    {
        ExecuteAsync($@"
            UPDATE
                {Config.DatabaseConnection.StorePlayersName}
            SET
                Vip = @Vip
            WHERE
                SteamID = @SteamID;
        ",
        new
        {
            Vip = AdminManager.PlayerHasPermissions(player, Config.Menu.VipFlag),
            SteamId = player.SteamID
        });
    }

    public static void LoadPlayer(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV || string.IsNullOrEmpty(player.IpAddress))
        {
            return;
        }

        Credits.SetOriginal(player, -1);
        Credits.Set(player, -1);
        ulong steamid = player.SteamID;
        string PlayerName = player.PlayerName;

        Task.Run(async () =>
        {
            await LoadPlayerAsync(player, steamid, PlayerName);
        });
    }

    public static async Task LoadPlayerAsync(CCSPlayerController player, ulong SteamID, string PlayerName)
    {
        async Task LoadDataAsync(int attempt = 1)
        {
            try
            {
                using MySqlConnection connection = await ConnectAsync();

                SqlMapper.GridReader multiQuery = await connection.QueryMultipleAsync($@"
                SELECT * FROM {Config.DatabaseConnection.StorePlayersName} WHERE SteamID = @SteamID;
                SELECT * FROM {Config.DatabaseConnection.StoreItemsName} WHERE SteamID = @SteamID AND(DateOfExpiration > @Now OR DateOfExpiration = '0001-01-01 00:00:00');
                SELECT * FROM " + Config.DatabaseConnection.StoreEquipments + @" WHERE SteamID = @SteamID"
                ,
                new
                {
                    SteamID,
                    DateTime.Now
                });

                Store_Player? playerData = await multiQuery.ReadFirstOrDefaultAsync<Store_Player>();

                IEnumerable<Store_Item> items = await multiQuery.ReadAsync<Store_Item>();

                IEnumerable<Store_Equipment> equipments = await multiQuery.ReadAsync<Store_Equipment>();

                Server.NextFrame(() =>
                {
                    if (playerData == null)
                    {
                        Store_Player newPlayer = new()
                        {
                            SteamID = SteamID,
                            PlayerName = PlayerName,
                            Credits = Config.Credits["default"].Start,
                            OriginalCredits = Config.Credits["default"].Start,
                            DateOfJoin = DateTime.Now,
                            DateOfLastJoin = DateTime.Now,
                            bPlayerIsLoaded = true,
                        };

                        Instance.GlobalStorePlayers.Add(newPlayer);
                        InsertNewPlayer(SteamID, PlayerName);
                    }
                    else
                    {
                        Store_Player? existingPlayer = Instance.GlobalStorePlayers.FirstOrDefault(p => p.SteamID == playerData.SteamID);

                        if (existingPlayer != null)
                        {
                            existingPlayer.PlayerName = playerData.PlayerName;
                            existingPlayer.Credits = Convert.ToInt32(playerData.Credits);
                            existingPlayer.OriginalCredits = existingPlayer.Credits;
                            existingPlayer.DateOfJoin = playerData.DateOfJoin;
                            existingPlayer.DateOfLastJoin = playerData.DateOfLastJoin;
                            existingPlayer.bPlayerIsLoaded = true;
                        }
                        else
                        {
                            playerData.OriginalCredits = Convert.ToInt32(playerData.Credits);

                            Instance.GlobalStorePlayers.Add(playerData);
                        }
                    }

                    foreach (Store_Item newItem in items)
                    {
                        Store_Item? existingItem = Instance.GlobalStorePlayerItems.FirstOrDefault(i => i.SteamID == newItem.SteamID && i.UniqueId == newItem.UniqueId && i.Type == newItem.Type);

                        if (existingItem != null)
                        {
                            existingItem.Price = newItem.Price;
                            existingItem.DateOfExpiration = newItem.DateOfExpiration;
                        }
                        else
                        {
                            Instance.GlobalStorePlayerItems.Add(newItem);
                        }
                    }

                    foreach (Store_Equipment newEquipment in equipments)
                    {
                        Store_Equipment? existingEquipment = Instance.GlobalStorePlayerEquipments.FirstOrDefault(e => e.SteamID == newEquipment.SteamID && e.UniqueId == newEquipment.UniqueId);
                        if (existingEquipment != null)
                        {
                            existingEquipment.Type = newEquipment.Type;
                            existingEquipment.Slot = newEquipment.Slot;
                        }
                        else
                        {
                            Instance.GlobalStorePlayerEquipments.Add(newEquipment);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Instance.Logger.LogError("Error load player {SteamID} attempt {attempt}: ex:{ErrorMessage}", SteamID, attempt, ex.Message);

                if (attempt < 3)
                {
                    Instance.Logger.LogInformation("Retrying to load player {SteamID} (attempt: {attempt})", SteamID, attempt + 1);
                    await Task.Delay(5000);
                    await LoadDataAsync(attempt + 1);
                }
                else
                {
                    Credits.SetOriginal(player, -1);
                    Credits.Set(player, -1);
                }
            }
        }

        await LoadDataAsync();
    }

    public static void InsertNewPlayer(ulong SteamId, string PlayerName)
    {
        ExecuteAsync($@"
                INSERT INTO {Config.DatabaseConnection.StorePlayersName} (
                    SteamID, PlayerName, Credits, DateOfJoin, DateOfLastJoin, Vip
                ) VALUES (
                    @SteamID, @PlayerName, @Credits, @DateOfJoin, @DateOfLastJoin, FALSE
                );"
            ,
            new
            {
                SteamId,
                PlayerName,
                Credits = Config.Credits["default"].Start,
                DateOfJoin = DateTime.Now,
                DateOfLastJoin = DateTime.Now
            });
    }

    public static void SavePlayer(CCSPlayerController player)
    {
        int PlayerCredits = Credits.Get(player);
        int PlayerOriginalCredits = Credits.GetOriginal(player);

        if (PlayerOriginalCredits == -1 || PlayerCredits == -1)
        {
            return;
        }

        int SetCredits = PlayerCredits - PlayerOriginalCredits;

        ExecuteAsync($@"
                UPDATE
                    {Config.DatabaseConnection.StorePlayersName}
                SET
                    PlayerName = @PlayerName,
                    Credits = GREATEST(Credits + @SetCredits, 0), 
                    DateOfJoin = @DateOfJoin, 
                    DateOfLastJoin = @DateOfLastJoin
                WHERE
                    SteamID = @SteamID;
            ",
            new
            {
                player.PlayerName,
                SetCredits,
                DateOfJoin = DateTime.Now,
                DateOfLastJoin = DateTime.Now,
                SteamId = player.SteamID,
            });

        Credits.SetOriginal(player, PlayerCredits);

    }

    public static void SavePlayerItem(CCSPlayerController player, Store_Item item)
    {
        ExecuteAsync($@"
                INSERT INTO {Config.DatabaseConnection.StoreItemsName} (
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
        ExecuteAsync($@"
                DELETE
                FROM
                    {Config.DatabaseConnection.StoreItemsName}
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
        ExecuteAsync(@"
                INSERT INTO " + Config.DatabaseConnection.StoreEquipments + @" (
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
        ExecuteAsync(@"
                    DELETE FROM " + Config.DatabaseConnection.StoreEquipments + @" WHERE SteamID = @SteamID AND UniqueId = @UniqueId;
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
        ExecuteAsync($@"
                DELETE FROM {Config.DatabaseConnection.StoreItemsName} WHERE SteamID = @SteamID; 
                DELETE FROM " + Config.DatabaseConnection.StoreEquipments + @" WHERE SteamID = @SteamID
            "
            ,
            new
            {
                player.SteamID
            });
    }

    public static void ResetDatabase()
    {
        Task.Run(async () =>
        {
            using MySqlConnection connection = await ConnectAsync();

            connection.Query($@"DROP TABLE {Config.DatabaseConnection.StorePlayersName}");
            connection.Query($@"DROP TABLE {Config.DatabaseConnection.StoreItemsName}");
            connection.Query($@"DROP TABLE {Config.DatabaseConnection.StoreEquipments}");
        });
    }
}