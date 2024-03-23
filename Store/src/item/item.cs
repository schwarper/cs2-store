using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using static Store.Store;
using static StoreApi.Store;

namespace Store;


public static class Item
{
    public static bool Purchase(CCSPlayerController player, Store_Item item)
    {
        if (Credits.Get(player) < item.Price)
        {
            player.PrintToChatMessage("No credits enough");
            return false;
        }

        Store_Item_Types? type = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item.Type);

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

        if (type.Equipable)
        {
            Store_PlayerItem playeritem = new()
            {
                SteamID = player.SteamID,
                Price = item.Price,
                Type = item.Type,
                UniqueId = item.UniqueId,
                Slot = item.Slot,
                Color = item.Color,
                DateOfPurchase = DateTime.Now
            };

            Instance.GlobalStorePlayerItems.Add(playeritem);

            Server.NextFrame(() =>
            {
                Database.SavePlayerItem(player, playeritem);
            });
        }
        else
        {
            if (!type.Equip(player, item))
            {
                return false;
            }
        }

        Credits.Give(player, -item.Price);

        player.PrintToChatMessage("Purchase Succeeded", item.Name);

        return true;
    }

    public static bool Equip(CCSPlayerController player, Store_Item item)
    {
        Store_Item_Types? type = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item.Type);

        if (type == null)
        {
            return false;
        }

        Store_PlayerItem? currentitem = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == item.Type && p.Slot == item.Slot);

        if (currentitem != null)
        {
            Store_Item citem = FindItemByUniqueId(currentitem.UniqueId)!;

            Unequip(player, citem);
        }

        if (type.Equip(player, item) == false)
        {
            return false;
        }

        Store_PlayerItem playeritem = new()
        {
            SteamID = player.SteamID,
            Type = item.Type,
            Price = item.Price,
            UniqueId = item.UniqueId,
            Slot = item.Slot,
            Color = item.Color
        };

        Instance.GlobalStorePlayerEquipments.Add(playeritem);

        Server.NextFrame(() =>
        {
            Database.SavePlayerEquipment(player, playeritem);
        });

        return true;
    }

    public static bool Unequip(CCSPlayerController player, Store_Item item)
    {
        Store_Item_Types? type = Instance.GlobalStoreItemTypes.FirstOrDefault(i => i.Type == item.Type);

        if (type == null)
        {
            return false;
        }

        if (type.Unequip(player, item) == false)
        {
            return false;
        }

        Instance.GlobalStorePlayerEquipments.RemoveAll(p => p.SteamID == player.SteamID && p.UniqueId == item.UniqueId);

        Server.NextFrame(() =>
        {
            Database.RemovePlayerEquipment(player, item.UniqueId);
        });

        return true;
    }

    public static bool Sell(CCSPlayerController player, Store_Item item)
    {
        Store_PlayerItem? playeritem = Instance.GlobalStorePlayerItems.FirstOrDefault(p => p.SteamID == player.SteamID && p.UniqueId == item.UniqueId);

        if (playeritem == null)
        {
            return false;
        }

        Unequip(player, item);

        Instance.GlobalStorePlayerItems.Remove(playeritem);

        Server.NextFrame(() =>
        {
            Database.RemovePlayerItem(player, playeritem);
        });

        return true;
    }

    public static bool PlayerHas(CCSPlayerController player, string UniqueId)
    {
        string? selectedItem = Instance.Config.Items.SelectMany(category => category.Value.Values)?.FirstOrDefault(item => item.UniqueId == UniqueId)?.Flag;

        bool flag = selectedItem != null && selectedItem != string.Empty && AdminManager.PlayerHasPermissions(player, selectedItem);

        return Instance.GlobalStorePlayerItems.Any(p => p.SteamID == player.SteamID && p.UniqueId == UniqueId) || flag;
    }

    public static bool PlayerUsing(CCSPlayerController player, string UniqueId)
    {
        return Instance.GlobalStorePlayerEquipments.Any(p => p.SteamID == player.SteamID && p.UniqueId == UniqueId);
    }

    public static bool IsInJson(string type, string UniqueId)
    {
        return Instance.Config.Items.Values
            .SelectMany(dict => dict.Values)
            .Any(item => item.Type == type && item.UniqueId == UniqueId);
    }

    public static Store_Item? FindItemByUniqueId(string UniqueId)
    {
        return Instance.Config.Items.Values
            .SelectMany(dict => dict.Values)
            .FirstOrDefault(item => item.UniqueId == UniqueId);
    }

    public static List<Store_PlayerItem> GetPlayerItems(CCSPlayerController player)
    {
        return Instance.GlobalStorePlayerItems.Where(item => item.SteamID == player.SteamID).ToList();
    }

    public static List<Store_PlayerItem> GetPlayerEquipments(CCSPlayerController player)
    {
        return Instance.GlobalStorePlayerEquipments.Where(item => item.SteamID == player.SteamID).ToList();
    }
}