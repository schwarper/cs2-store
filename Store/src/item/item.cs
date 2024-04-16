using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using System.Globalization;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item
{
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
            player.PrintToChatMessage("No type found");
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

        if (!type.Equip(player, item))
        {
            return false;
        }

        Credits.Give(player, -int.Parse(item["price"]));

        player.PrintToChatMessage("Purchase Succeeded", item["name"]);

        if (type.Equipable)
        {
            item.TryGetValue("expiration", out string? expirationtime);

            int expiration = Convert.ToInt32(expirationtime);

            Store_Item playeritem = new()
            {
                SteamID = player.SteamID,
                Price = int.Parse(item["price"]),
                Type = item["type"],
                UniqueId = item["uniqueid"],
                DateOfPurchase = DateTime.Now,
                DateOfExpiration = expiration <= 0 ? DateTime.MinValue : DateTime.Now.AddSeconds(expiration)
            };

            Instance.GlobalStorePlayerItems.Add(playeritem);

            Server.NextFrame(() =>
            {
                Database.SavePlayerItem(player, playeritem);
            });
        }
        else
        {
            return false;
        }

        return true;
    }

    public static bool Equip(CCSPlayerController player, Dictionary<string, string> item)
    {
        Store_Item_Types? type = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item["type"]);

        if (type == null)
        {
            return false;
        }

        Store_Equipment? currentitem = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == item["type"] && item.TryGetValue("slot", out string? slot) && !string.IsNullOrEmpty(slot) && p.Slot == int.Parse(item["slot"]));

        if (currentitem != null)
        {
            Dictionary<string, string> citem = Find(currentitem.Type, currentitem.UniqueId)!;

            Unequip(player, citem);
        }

        if (type.Equip(player, item) == false)
        {
            return false;
        }

        if (!item.TryGetValue("slot", out string? sslot) || !int.TryParse(sslot, out int islot))
        {
            islot = 0;
        }

        Store_Equipment playeritem = new()
        {
            SteamID = player.SteamID,
            Type = item["type"],
            UniqueId = item["uniqueid"],
            Slot = islot
        };

        Instance.GlobalStorePlayerEquipments.Add(playeritem);

        Server.NextFrame(() =>
        {
            Database.SavePlayerEquipment(player, playeritem);
        });

        return true;
    }

    public static bool Unequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        Store_Item_Types? type = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item["type"]);

        if (type == null)
        {
            return false;
        }

        if (type.Unequip(player, item) == false)
        {
            return false;
        }

        Instance.GlobalStorePlayerEquipments.RemoveAll(p => p.SteamID == player.SteamID && p.UniqueId == item["uniqueid"]);

        Server.NextFrame(() =>
        {
            Database.RemovePlayerEquipment(player, item["uniqueid"]);
        });

        return true;
    }

    public static bool Sell(CCSPlayerController player, Dictionary<string, string> item)
    {
        Store_Item? playeritem = Instance.GlobalStorePlayerItems.FirstOrDefault(p => p.SteamID == player.SteamID && p.UniqueId == item["uniqueid"]);

        if (playeritem == null)
        {
            return false;
        }

        float sell_ratio = 1.0f;

        if (Instance.Config.Settings.TryGetValue("sell_ratio", out string? value) && float.TryParse(value, CultureInfo.InvariantCulture, out float ratio))
        {
            sell_ratio = ratio;
        }

        Credits.Give(player, (int)(playeritem.Price * sell_ratio));

        Unequip(player, item);

        Instance.GlobalStorePlayerItems.Remove(playeritem);

        Server.NextFrame(() =>
        {
            Database.RemovePlayerItem(player, playeritem);
        });

        return true;
    }

    public static bool PlayerHas(CCSPlayerController player, string type, string UniqueId, bool ignoreVip)
    {
        Dictionary<string, string>? item = Find(type, UniqueId);

        if (item == null)
        {
            return false;
        }

        if (!ignoreVip && IsPlayerVip(player))
        {
            return true;
        }

        if (item.TryGetValue("flag", out string? flag) && !string.IsNullOrEmpty(flag) && AdminManager.PlayerHasPermissions(player, flag))
        {
            return true;
        }

        return Instance.GlobalStorePlayerItems.Any(p => p.SteamID == player.SteamID && p.Type == type && p.UniqueId == UniqueId);
    }

    public static bool PlayerUsing(CCSPlayerController player, string type, string UniqueId)
    {
        return Instance.GlobalStorePlayerEquipments.Any(p => p.SteamID == player.SteamID && p.Type == type && p.UniqueId == UniqueId);
    }

    public static bool IsInJson(string type, string UniqueId)
    {
        return Instance.Config.Items.Values
            .SelectMany(dict => dict.Values)
            .Any(item => item["type"] == type && item["uniqueid"] == UniqueId);
    }

    public static Dictionary<string, string>? Find(string type, string UniqueId)
    {
        return Instance.Config.Items.Values
            .SelectMany(dict => dict.Values)
            .FirstOrDefault(item => item["type"] == type && item["uniqueid"] == UniqueId);
    }

    public static List<Store_Item> GetPlayerItems(CCSPlayerController player)
    {
        return Instance.GlobalStorePlayerItems.Where(item => item.SteamID == player.SteamID).ToList();
    }

    public static List<Store_Equipment> GetPlayerEquipments(CCSPlayerController player)
    {
        return Instance.GlobalStorePlayerEquipments.Where(item => item.SteamID == player.SteamID).ToList();
    }

    public static bool IsPlayerVip(CCSPlayerController player)
    {
        string vip = Instance.Config.Menu["vip_flag"];

        return !string.IsNullOrEmpty(vip) && AdminManager.PlayerHasPermissions(player, vip);
    }

    public static void RegisterType(string Type, Action MapStart, Func<CCSPlayerController, Dictionary<string, string>, bool> Equip, Func<CCSPlayerController, Dictionary<string, string>, bool> Unequip, bool Equipable, bool? Alive)
    {
        Instance.GlobalStoreItemTypes.Add(new Store_Item_Types
        {
            Type = Type,
            MapStart = MapStart,
            Equip = Equip,
            Unequip = Unequip,
            Equipable = Equipable,
            Alive = Alive
        });
    }

    public static List<KeyValuePair<string, Dictionary<string, string>>> GetItemsByType(string type)
    {
        return Instance.Config.Items
        .SelectMany(wk => wk.Value)
        .Where(kvp => kvp.Value["type"] == type).ToList();
    }
}