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
        if (!ItemModuleManager.Modules.TryGetValue(item["type"], out IItemModule? type))
        {
            player.PrintToChatMessage("No type found", item["type"]);
            return false;
        }

        if (!type.Equipable && !type.OnEquip(player, item)) return false;

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

        if (!ItemModuleManager.Modules.TryGetValue(item["type"], out IItemModule? type))
            return false;

        if (type.RequiresAlive == true && !player.PawnIsAlive)
            return false;

        else if (type.RequiresAlive == false && player.PawnIsAlive)
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

        if (!ItemModuleManager.Modules.TryGetValue(item["type"], out IItemModule? type))
        {
            player.PrintToChatMessage("No type found", item["type"]);
            return false;
        }

        if (type.RequiresAlive == true && !player.PawnIsAlive)
        {
            player.PrintToChatMessage("You are not alive");
            return false;
        }
        else if (type.RequiresAlive == false && player.PawnIsAlive)
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

            if (!type.OnEquip(player, item))
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
        string itemType = item["type"];

        if (!ItemModuleManager.Modules.TryGetValue(itemType, out IItemModule? type))
            return false;

        if (item.TryGetValue("team", out string? steam) && int.TryParse(steam, out int team) && team >= 1 && team <= 3 && player.TeamNum != team)
        {
            player.PrintToChatMessage("No equip because team", (CsTeam)team);
            return false;
        }

        // 根据物品类型采用不同的冲突检测逻辑
        List<Store_Equipment> currentItems = GetConflictingItems(player, item, itemType);

        foreach (Store_Equipment currentItem in currentItems)
        {
            Dictionary<string, string>? citem = GetItem(currentItem.UniqueId);
            if (citem != null) Unequip(player, citem, false);
        }

        if (!type.OnEquip(player, item)) return false;

        int slot = item.TryGetValue("slot", out string? sslot) && int.TryParse(sslot, out int islot) ? islot : 0;

        Store_Equipment playerItem = new()
        {
            SteamID = player.SteamID,
            Type = itemType,
            UniqueId = item["uniqueid"],
            Slot = slot
        };

        Instance.GlobalStorePlayerEquipments.Add(playerItem);
        Store.Api.PlayerEquipItem(player, item);
        Server.NextFrame(() => Database.SavePlayerEquipment(player, playerItem));

        return true;
    }

    /// <summary>
    /// 根据物品类型获取冲突的已装备物品
    /// </summary>
    private static List<Store_Equipment> GetConflictingItems(CCSPlayerController player, Dictionary<string, string> item, string itemType)
    {
        return itemType switch
        {
            // playerskin: 同类型且同slot的物品冲突（队伍限制）
            "playerskin" => [.. Instance.GlobalStorePlayerEquipments.FindAll(p =>
                p.SteamID == player.SteamID &&
                p.Type == itemType &&
                (p.Slot == int.Parse(item["slot"]) || (item["slot"] == "1" || p.Slot == 1)))],

            // customweapon: 同武器类型的物品冲突（基于weapon属性，确保同一武器只能装备一个皮肤）
            "customweapon" => [.. Instance.GlobalStorePlayerEquipments.FindAll(p =>
            {
                if (p.SteamID != player.SteamID || p.Type != itemType)
                    return false;
                
                Dictionary<string, string>? equippedItem = GetItem(p.UniqueId);
                return equippedItem != null && 
                       equippedItem.TryGetValue("weapon", out string? equippedWeapon) &&
                       item.TryGetValue("weapon", out string? newWeapon) &&
                       equippedWeapon == newWeapon;
            })],

            // equipment: 同类型且同slot的物品冲突
            "equipment" => [.. Instance.GlobalStorePlayerEquipments.FindAll(p =>
                p.SteamID == player.SteamID &&
                p.Type == itemType &&
                p.Slot == int.Parse(item["slot"]))],

            // 其他类型: 默认采用同类型且同slot冲突的逻辑
            _ => [.. Instance.GlobalStorePlayerEquipments.FindAll(p =>
                p.SteamID == player.SteamID &&
                p.Type == itemType &&
                p.Slot == int.Parse(item["slot"]))]
        };
    }

    public static bool Unequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        if (!ItemModuleManager.Modules.TryGetValue(item["type"], out IItemModule? type))
            return false;

        Store_Equipment? equippedItem = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.UniqueId == item["uniqueid"]);
        if (equippedItem == null) return false;

        Instance.GlobalStorePlayerEquipments.Remove(equippedItem);
        Store.Api.PlayerUnequipItem(player, item);
        Server.NextFrame(() => Database.RemovePlayerEquipment(player, item["uniqueid"]));

        return type.OnUnequip(player, item, update);
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

        if (!ItemModuleManager.Modules.TryGetValue(item["type"], out IItemModule? moduletype))
            return false;

        if (moduletype.Equipable == false) return false;

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