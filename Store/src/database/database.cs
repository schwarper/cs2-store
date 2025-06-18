using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using static Store.ConfigConfig;
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

    public static async Task CreateDatabaseAsync(ConfigDatabaseConnection config)
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
            string storePlayers = config.StorePlayersName;
            string storeİtems = config.StoreItemsName;
            string storeEquipments = config.StoreEquipments;

            await connection.ExecuteAsync($@"
                CREATE TABLE IF NOT EXISTS {storePlayers} (
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
                CREATE TABLE IF NOT EXISTS {storeİtems} (
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
                CREATE TABLE IF NOT EXISTS {storeEquipments} (
                    id INT NOT NULL AUTO_INCREMENT,
                    SteamID BIGINT UNSIGNED NOT NULL,
                    Type varchar(16) NOT NULL,
                    UniqueId varchar(256) NOT NULL,
                    Slot INT,
                    PRIMARY KEY (id)
                );", transaction: transaction);

            await connection.ExecuteAsync($@"
                DELETE FROM {storeEquipments}
                    WHERE NOT EXISTS (
                        SELECT 1 FROM {storeİtems}
                        WHERE {storeİtems}.Type = {storeEquipments}.Type
                        AND {storeİtems}.UniqueId = {storeEquipments}.UniqueId
                        AND {storeİtems}.SteamID = {storeEquipments}.SteamID
                    )
                    AND EXISTS (
                        SELECT 1 
                        FROM {storePlayers}
                        WHERE {storePlayers}.SteamID = {storeEquipments}.SteamID
                        AND {storePlayers}.Vip = FALSE
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
        string playerName = player.PlayerName;

        Task.Run(async () =>
        {
            await LoadPlayerAsync(player, steamid, playerName);
        });
    }

    public static async Task LoadPlayerAsync(CCSPlayerController player, ulong steamId, string playerName)
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
                    SteamID = steamId,
                    DateTime.Now
                });

                StorePlayer? playerData = await multiQuery.ReadFirstOrDefaultAsync<StorePlayer>();

                var items = await multiQuery.ReadAsync<StoreItem>();

                var equipments = await multiQuery.ReadAsync<StoreEquipment>();

                Server.NextFrame(() =>
                {
                    if (playerData == null)
                    {
                        StorePlayer newPlayer = new()
                        {
                            SteamId = steamId,
                            PlayerName = playerName,
                            Credits = Config.Credits["default"].Start,
                            OriginalCredits = Config.Credits["default"].Start,
                            DateOfJoin = DateTime.Now,
                            DateOfLastJoin = DateTime.Now,
                            BPlayerIsLoaded = true,
                        };

                        Instance.GlobalStorePlayers.Add(newPlayer);
                        InsertNewPlayer(steamId, playerName);
                    }
                    else
                    {
                        StorePlayer? existingPlayer = Instance.GlobalStorePlayers.FirstOrDefault(p => p.SteamId == playerData.SteamId);

                        if (existingPlayer != null)
                        {
                            existingPlayer.PlayerName = playerData.PlayerName;
                            existingPlayer.Credits = Convert.ToInt32(playerData.Credits);
                            existingPlayer.OriginalCredits = existingPlayer.Credits;
                            existingPlayer.DateOfJoin = playerData.DateOfJoin;
                            existingPlayer.DateOfLastJoin = playerData.DateOfLastJoin;
                            existingPlayer.BPlayerIsLoaded = true;
                        }
                        else
                        {
                            playerData.OriginalCredits = Convert.ToInt32(playerData.Credits);

                            Instance.GlobalStorePlayers.Add(playerData);
                        }
                    }

                    foreach (StoreItem newItem in items)
                    {
                        StoreItem? existingItem = Instance.GlobalStorePlayerItems.FirstOrDefault(i => i.SteamId == newItem.SteamId && i.UniqueId == newItem.UniqueId && i.Type == newItem.Type);

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

                    foreach (StoreEquipment newEquipment in equipments)
                    {
                        StoreEquipment? existingEquipment = Instance.GlobalStorePlayerEquipments.FirstOrDefault(e => e.SteamId == newEquipment.SteamId && e.UniqueId == newEquipment.UniqueId);
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
                Instance.Logger.LogError("Error load player {SteamID} attempt {attempt}: ex:{ErrorMessage}", steamId, attempt, ex.Message);

                if (attempt < 3)
                {
                    Instance.Logger.LogInformation("Retrying to load player {SteamID} (attempt: {attempt})", steamId, attempt + 1);
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

    public static void InsertNewPlayer(ulong steamId, string playerName)
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
                SteamId = steamId,
                PlayerName = playerName,
                Credits = Config.Credits["default"].Start,
                DateOfJoin = DateTime.Now,
                DateOfLastJoin = DateTime.Now
            });
    }

    public static void SavePlayer(CCSPlayerController player)
    {
        int playerCredits = Credits.Get(player);
        int playerOriginalCredits = Credits.GetOriginal(player);

        if (playerOriginalCredits == -1 || playerCredits == -1)
        {
            return;
        }

        int setCredits = playerCredits - playerOriginalCredits;

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
                SetCredits = setCredits,
                DateOfJoin = DateTime.Now,
                DateOfLastJoin = DateTime.Now,
                SteamId = player.SteamID,
            });

        Credits.SetOriginal(player, playerCredits);

    }

    public static void SavePlayerItem(CCSPlayerController player, StoreItem item)
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
    public static void RemovePlayerItem(CCSPlayerController player, StoreItem item)
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
    public static void SavePlayerEquipment(CCSPlayerController player, StoreEquipment item)
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
    public static void RemovePlayerEquipment(CCSPlayerController player, string uniqueId)
    {
        ExecuteAsync(@"
                    DELETE FROM " + Config.DatabaseConnection.StoreEquipments + @" WHERE SteamID = @SteamID AND UniqueId = @UniqueId;
                "
            ,
            new
            {
                player.SteamID, UniqueId = uniqueId
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