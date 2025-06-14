using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using System.Text.Json;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item
{
    public static bool IsHidden(this Dictionary<string, string> item)
    {
        return item.ContainsKey("hide") && item["hide"] == "true";
    }

    public static string GetItemName(CCSPlayerController player, Dictionary<string, string> item)
    {
        string name = item["name"];

        return name.StartsWith('*') && name.EndsWith('*') ? Instance.Localizer.ForPlayer(player, name) : name;
    }

    public static bool Give(CCSPlayerController player, Dictionary<string, string> item)
    {
        Store_Item_Types? type = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item["type"]);
        if (type == null)
        {
            player.PrintToChatMessage("No type found", item["type"]);
            return false;
        }

        if (!type.Equipable && !type.Equip(player, item)) return false;

        if (type.Equipable)
        {
            item.TryGetValue("expiration", out string? expirationtime);
            int expiration = Convert.ToInt32(expirationtime);

            Store_Item playerItem = new()
            {
                SteamID = player.SteamID,
                Price = int.Parse(item["price"]),
                Type = item["type"],
                UniqueId = item["uniqueid"],
                DateOfPurchase = DateTime.Now,
                DateOfExpiration = expiration <= 0 ? DateTime.MinValue : DateTime.Now.AddSeconds(expiration)
            };

            Instance.GlobalStorePlayerItems.Add(playerItem);
            Server.NextFrame(() => Database.SavePlayerItem(player, playerItem));
        }
        else
        {
            return false;
        }

        return true;
    }

    public static bool CanBuy(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (Credits.Get(player) < int.Parse(item["price"]))
            return false;

        Store_Item_Types? type = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item["type"]);

        if (type == null)
            return false;

        if (type.Alive == true && !player.PawnIsAlive)
            return false;

        else if (type.Alive == false && player.PawnIsAlive)
            return false;

        if (!type.Equipable)
        {
            if (item.TryGetValue("team", out string? steam) && int.TryParse(steam, out int team) && team >= 1 && team <= 3 && player.TeamNum != team)
                return false;
        }

        return true;
    }

    public static bool Purchase(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (Credits.Get(player) < int.Parse(item["price"]))
        {
            player.PrintToChatMessage("No credits enough");
            return false;
        }

        Store_Item_Types? type = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item["type"]);

        if (type == null)
        {
            player.PrintToChatMessage("No type found", item["type"]);
            return false;
        }

        if (type.Alive == true && !player.PawnIsAlive)
        {
            player.PrintToChatMessage("You are not alive");
            return false;
        }
        else if (type.Alive == false && player.PawnIsAlive)
        {
            player.PrintToChatMessage("You are alive");
            return false;
        }

        if (!type.Equipable)
        {
            if (item.TryGetValue("team", out string? steam) && int.TryParse(steam, out int team) && team >= 1 && team <= 3 && player.TeamNum != team)
            {
                player.PrintToChatMessage("No purchase because team", (CsTeam)team);
                return false;
            }

            if (!type.Equip(player, item))
                return false;
        }

        int price = int.Parse(item["price"]);
        if (price > 0)
        {
            Credits.Give(player, -price);
            player.PrintToChatMessage("Purchase Succeeded", GetItemName(player, item));
        }

        Store.Api.PlayerPurchaseItem(player, item);

        if (type.Equipable)
        {
            item.TryGetValue("expiration", out string? expirationtime);
            int expiration = Convert.ToInt32(expirationtime);

            Store_Item playerItem = new()
            {
                SteamID = player.SteamID,
                Price = price,
                Type = item["type"],
                UniqueId = item["uniqueid"],
                DateOfPurchase = DateTime.Now,
                DateOfExpiration = expiration <= 0 ? DateTime.MinValue : DateTime.Now.AddSeconds(expiration)
            };

            Instance.GlobalStorePlayerItems.Add(playerItem);
            Store.Api.PlayerEquipItem(player, item);
            Server.NextFrame(() => Database.SavePlayerItem(player, playerItem));
        }
        else return false;

        return true;
    }

    public static bool Equip(CCSPlayerController player, Dictionary<string, string> item)
    {
        Store_Item_Types? type = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item["type"]);
        if (type == null) return false;

        if (item.TryGetValue("team", out string? steam) && int.TryParse(steam, out int team) && team >= 1 && team <= 3 && player.TeamNum != team)
        {
            player.PrintToChatMessage("No equip because team", (CsTeam)team);
            return false;
        }

        List<Store_Equipment> currentItems = [.. Instance.GlobalStorePlayerEquipments.FindAll(p =>
            p.SteamID == player.SteamID &&
            p.Type == type.Type &&
            (p.Slot == int.Parse(item["slot"]) ||
            type.Type == "playerskin" && (item["slot"] == "1" || p.Slot == 1)))];

        foreach (Store_Equipment currentItem in currentItems)
        {
            Dictionary<string, string>? citem = GetItem(currentItem.UniqueId);
            if (citem != null) Unequip(player, citem, false);
        }

        if (!type.Equip(player, item)) return false;

        int slot = item.TryGetValue("slot", out string? sslot) && int.TryParse(sslot, out int islot) ? islot : 0;

        Store_Equipment playerItem = new()
        {
            SteamID = player.SteamID,
            Type = item["type"],
            UniqueId = item["uniqueid"],
            Slot = slot
        };

        Instance.GlobalStorePlayerEquipments.Add(playerItem);
        Store.Api.PlayerEquipItem(player, item);
        Server.NextFrame(() => Database.SavePlayerEquipment(player, playerItem));

        return true;
    }

    public static bool Unequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        Store_Item_Types? type = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item["type"]);
        if (type == null) return false;

        Store_Equipment? equippedItem = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.UniqueId == item["uniqueid"]);
        if (equippedItem == null) return false;

        Instance.GlobalStorePlayerEquipments.Remove(equippedItem);
        Store.Api.PlayerUnequipItem(player, item);
        Server.NextFrame(() => Database.RemovePlayerEquipment(player, item["uniqueid"]));

        return type.Unequip(player, item, update);
    }

    public static bool Sell(CCSPlayerController player, Dictionary<string, string> item)
    {
        Store_Item? playerItem = Instance.GlobalStorePlayerItems.FirstOrDefault(p => p.SteamID == player.SteamID && p.UniqueId == item["uniqueid"]);
        if (playerItem == null) return false;

        Credits.Give(player, (int)(playerItem.Price * Config.Settings.SellRatio));
        Unequip(player, item, true);
        Instance.GlobalStorePlayerItems.Remove(playerItem);
        Store.Api.PlayerSellItem(player, item);
        Server.NextFrame(() => Database.RemovePlayerItem(player, playerItem));

        return true;
    }

    public static bool PlayerHas(CCSPlayerController player, string type, string uniqueId, bool ignoreVip)
    {
        Dictionary<string, string>? item = GetItem(uniqueId);
        if (item == null) return false;

        Store_Item_Types? itemType = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item["type"]);
        if (itemType?.Equipable == false) return false;

        if (!ignoreVip && IsPlayerVip(player)) return true;

        item.TryGetValue("flag", out string? flag);
        return MenuBase.CheckFlag(player, flag, false) || Instance.GlobalStorePlayerItems.Any(p => p.SteamID == player.SteamID && p.Type == type && p.UniqueId == uniqueId);
    }

    public static bool PlayerUsing(CCSPlayerController player, string type, string uniqueId)
    {
        return Instance.GlobalStorePlayerEquipments.Any(p => p.SteamID == player.SteamID && p.Type == type && p.UniqueId == uniqueId);
    }

    public static bool IsInJson(string uniqueId)
    {
        return Instance.Items.ContainsKey(uniqueId);
    }

    public static Dictionary<string, string>? GetItem(string uniqueId)
    {
        return Instance.Items.TryGetValue(uniqueId, out Dictionary<string, string>? item) ? item : null;
    }

    public static bool IsAnyItemExistInType(string type)
    {
        return Instance.Items.Any(kvp => kvp.Value["type"] == type);
    }

    public static bool IsAnyItemExistInTypes(string[] type)
    {
        return Instance.Items.Any(kvp => type.Contains(kvp.Value["type"]));
    }

    public static List<KeyValuePair<string, Dictionary<string, string>>> GetItemsByType(string type)
    {
        return [.. Instance.Items.Where(kvp => kvp.Value["type"] == type)];
    }

    public static List<Store_Item> GetPlayerItems(CCSPlayerController player, string? type)
    {
        return [.. Instance.GlobalStorePlayerItems.Where(item => item.SteamID == player.SteamID && (type == null || type == item.Type))];
    }

    public static List<Store_Equipment> GetPlayerEquipments(CCSPlayerController player, string? type)
    {
        return [.. Instance.GlobalStorePlayerEquipments.Where(item => item.SteamID == player.SteamID && (type == null || type == item.Type))];
    }

    public static bool IsPlayerVip(CCSPlayerController player)
    {
        return !string.IsNullOrEmpty(Config.Menu.VipFlag) && AdminManager.PlayerHasPermissions(player, Config.Menu.VipFlag);
    }

    public static bool PlayerHasAny(CCSPlayerController player, JsonElement item)
    {
        return item.ExtractItems().Values.Any(item => PlayerHas(player, item["type"], item["uniqueid"], false));
    }

    public static void RegisterType(string Type, Action MapStart, Action<ResourceManifest> ServerPrecacheResources, Func<CCSPlayerController, Dictionary<string, string>, bool> Equip, Func<CCSPlayerController, Dictionary<string, string>, bool, bool> Unequip, bool Equipable, bool? Alive)
    {
        Instance.GlobalStoreItemTypes.Add(new Store_Item_Types
        {
            Type = Type,
            MapStart = MapStart,
            ServerPrecacheResources = ServerPrecacheResources,
            Equip = Equip,
            Unequip = Unequip,
            Equipable = Equipable,
            Alive = Alive
        });
    }

    public static void RemoveExpiredItems()
    {
        Database.ExecuteAsync($"DELETE FROM {Config.DatabaseConnection.StoreItemsName} WHERE DateOfExpiration < NOW() AND DateOfExpiration > '0001-01-01 00:00:00';", null);

        List<Store_Item> itemsToRemove = [.. Instance.GlobalStorePlayerItems.Where(item => item.DateOfExpiration < DateTime.Now && item.DateOfExpiration > DateTime.MinValue)];

        string storeEquipmentTableName = Config.DatabaseConnection.StoreEquipments;

        foreach (Store_Item? item in itemsToRemove)
        {
            Database.ExecuteAsync($"DELETE FROM {storeEquipmentTableName} WHERE SteamID = @SteamID AND UniqueId = @UniqueId", new { item.SteamID, item.UniqueId });

            Instance.GlobalStorePlayerItems.Remove(item);
            Instance.GlobalStorePlayerEquipments.RemoveAll(i => i.UniqueId == item.UniqueId);
        }
    }
}