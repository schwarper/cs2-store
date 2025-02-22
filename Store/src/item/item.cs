using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item
{
    public static bool IsHidden(this Dictionary<string, string> item) => item.ContainsKey("hide") && item["hide"] == "true";

    public static bool Give(CCSPlayerController player, Dictionary<string, string> item)
    {
        Store_Item_Types? type = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item["type"]);

        if (type == null)
        {
            player.PrintToChatMessage("No type found");
            return false;
        }

        if (!type.Equipable && !type.Equip(player, item))
        {
            return false;
        }

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

        if (!type.Equipable)
        {
            if (item.TryGetValue("team", out string? steam) && int.TryParse(steam, out int team) && team >= 1 && team <= 3 && player.TeamNum != team)
            {
                player.PrintToChatMessage("No purchase because team", (CsTeam)team);
                return false;
            }

            if (!type.Equip(player, item))
            {
                return false;
            }
        }

        int price = int.Parse(item["price"]);

        if (price > 0)
        {
            Credits.Give(player, -int.Parse(item["price"]));

            player.PrintToChatMessage("Purchase Succeeded", item["name"]);
        }

        Store.Api.PlayerPurchaseItem(player, item);

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

            Store.Api.PlayerEquipItem(player, item);

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

        if (item.TryGetValue("team", out string? steam) && int.TryParse(steam, out int team) && team >= 1 && team <= 3 && player.TeamNum != team)
        {
            player.PrintToChatMessage("No equip because team", (CsTeam)team);
            return false;
        }

        List<Store_Equipment> currentitems = [.. Instance.GlobalStorePlayerEquipments.FindAll(p =>
            p.SteamID == player.SteamID &&
            p.Type == type.Type &&
            ((type.Type == "playerskin" && (item["slot"] == "1" || p.Slot == 1)) ||
            p.Slot == int.Parse(item["slot"]))
        )];

        if (currentitems.Count > 0)
        {
            foreach (Store_Equipment? currentitem in currentitems)
            {
                Dictionary<string, string>? citem = GetItem(currentitem.UniqueId);

                if (citem != null)
                {
                    Unequip(player, citem, false);
                }
            }
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

        Store.Api.PlayerEquipItem(player, item);

        Server.NextFrame(() =>
        {
            Database.SavePlayerEquipment(player, playeritem);
        });

        return true;
    }

    public static bool Unequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        Store_Item_Types? type = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item["type"]);

        if (type == null)
        {
            return false;
        }

        var equippedItem = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.UniqueId == item["uniqueid"]);

        if (equippedItem == null)
        {
            return false;
        }

        if (type.Unequip(player, item, update) == false)
        {
            return false;
        }

        Instance.GlobalStorePlayerEquipments.Remove(equippedItem);

        Store.Api.PlayerUnequipItem(player, item);

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

        Credits.Give(player, (int)(playeritem.Price * Config.Settings.SellRatio));

        Unequip(player, item, true);

        Instance.GlobalStorePlayerItems.Remove(playeritem);

        Store.Api.PlayerSellItem(player, item);

        Server.NextFrame(() =>
        {
            Database.RemovePlayerItem(player, playeritem);
        });

        return true;
    }

    public static bool PlayerHas(CCSPlayerController player, string type, string uniqueId, bool ignoreVip)
    {
        Dictionary<string, string>? item = GetItem(uniqueId);

        if (item == null)
        {
            return false;
        }

        Store_Item_Types? itemtype = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item["type"]);

        if (itemtype?.Equipable == false)
        {
            return false;
        }

        if (!ignoreVip && IsPlayerVip(player))
        {
            return true;
        }

        item.TryGetValue("flag", out string? flag);

        if (Menu.CheckFlag(player, flag, false))
        {
            return true;
        }

        return Instance.GlobalStorePlayerItems.Any(p => p.SteamID == player.SteamID && p.Type == type && p.UniqueId == uniqueId);
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
        Instance.Items.TryGetValue(uniqueId, out var item);
        return item;
    }

    public static List<KeyValuePair<string, Dictionary<string, string>>> GetItemsByType(string type)
    {
        return [.. Instance.Items.Where(kvp => kvp.Value["type"] == type)];
    }

    public static List<Store_Item> GetPlayerItems(CCSPlayerController player)
    {
        return [.. Instance.GlobalStorePlayerItems.Where(item => item.SteamID == player.SteamID)];
    }

    public static List<Store_Equipment> GetPlayerEquipments(CCSPlayerController player)
    {
        return [.. Instance.GlobalStorePlayerEquipments.Where(item => item.SteamID == player.SteamID)];
    }

    public static bool IsPlayerVip(CCSPlayerController player)
    {
        string vip = Config.Menu.VipFlag;

        return !string.IsNullOrEmpty(vip) && AdminManager.PlayerHasPermissions(player, vip);
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
}
