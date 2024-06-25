using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Database
{
    public static string GlobalDatabaseConnectingString { get; set; } = string.Empty;

    public static async Task<MySqlConnection> ConnectAsync()
    {
        MySqlConnection connection = new(GlobalDatabaseConnectingString);
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

    public static async Task CreateDatabaseAsync(StoreConfig config)
    {
        MySqlConnectionStringBuilder builder = new()
        {
            Server = config.Database.Host,
            Database = config.Database.Name,
            UserID = config.Database.User,
            Password = config.Database.Password,
            Port = config.Database.Port,
            Pooling = true,
            MinimumPoolSize = 0,
            MaximumPoolSize = 640,
            ConnectionIdleTimeout = 30,
            AllowZeroDateTime = true
        };

        GlobalDatabaseConnectingString = builder.ConnectionString;

        using MySqlConnection connection = await ConnectAsync();
        using MySqlTransaction transaction = await connection.BeginTransactionAsync();

        try
        {
            string equipTableName = config.Settings.DatabaseEquipTableName;

            await connection.ExecuteAsync(@"
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

            await connection.ExecuteAsync(@"
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

            await connection.ExecuteAsync($@"
                CREATE TABLE IF NOT EXISTS {equipTableName} (
                    id INT NOT NULL AUTO_INCREMENT,
                    SteamID BIGINT UNSIGNED NOT NULL,
                    Type varchar(16) NOT NULL,
                    UniqueId varchar(256) NOT NULL,
                    Slot INT,
                    PRIMARY KEY (id)
                );", transaction: transaction);

            await connection.ExecuteAsync($@"
                DELETE FROM {equipTableName}
                    WHERE NOT EXISTS (
                    SELECT 1 FROM store_items
                    WHERE store_items.Type = {equipTableName}.Type
                    AND store_items.UniqueId = {equipTableName}.UniqueId
                    AND store_items.SteamID = {equipTableName}.SteamID
                )", transaction: transaction);

            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
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

                SqlMapper.GridReader multiQuery = await connection.QueryMultipleAsync(@"
                SELECT * FROM store_players WHERE SteamID = @SteamID;
                SELECT * FROM store_items WHERE SteamID = @SteamID AND(DateOfExpiration > @Now OR DateOfExpiration = '0001-01-01 00:00:00');
                SELECT * FROM " + Instance.Config.Settings.DatabaseEquipTableName + @" WHERE SteamID = @SteamID"
                ,
                new
                {
                    SteamID,
                    DateTime.Now
                });

                Store_Player playerData = await multiQuery.ReadFirstOrDefaultAsync<Store_Player>();

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
                            Credits = Instance.Config.Credits.Start,
                            OriginalCredits = Instance.Config.Credits.Start,
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
        ExecuteAsync(@"
                INSERT INTO store_players (
                    SteamID, PlayerName, Credits, DateOfJoin, DateOfLastJoin
                ) VALUES (
                    @SteamID, @PlayerName, @Credits, @DateOfJoin, @DateOfLastJoin
                );"
            ,
            new
            {
                SteamId,
                PlayerName,
                Credits = Instance.Config.Credits.Start,
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

        ExecuteAsync(@"
                UPDATE
                    store_players
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
        ExecuteAsync(@"
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
        ExecuteAsync(@"
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
        ExecuteAsync(@"
                INSERT INTO " + Instance.Config.Settings.DatabaseEquipTableName + @" (
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
                    DELETE FROM " + Instance.Config.Settings.DatabaseEquipTableName + @" WHERE SteamID = @SteamID AND UniqueId = @UniqueId;
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
        ExecuteAsync(@"
                DELETE FROM store_items WHERE SteamID = @SteamID; 
                DELETE FROM " + Instance.Config.Settings.DatabaseEquipTableName + @" WHERE SteamID = @SteamID
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

            connection.Query(@"DROP TABLE store_players");
            connection.Query(@"DROP TABLE store_items");
            connection.Query($@"DROP TABLE {Instance.Config.Settings.DatabaseEquipTableName}");
        });
    }
}