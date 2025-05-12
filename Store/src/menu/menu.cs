using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Enum;
using CS2MenuManager.API.Interface;
using CS2MenuManager.API.Menu;
using System.Text.Json;
using static Store.Config_Config;
using static Store.MenuBase;
using static Store.Store;

namespace Store;

public static class Menu
{
    public static void DisplayStore(CCSPlayerController player, bool inventory)
    {
        OpenMenu(player, Instance.Localizer.ForPlayer(player, "menu_store<title>", Credits.Get(player)), Instance.Config.Items, inventory, null);
    }

    public static void OpenMenu(CCSPlayerController player, string title, JsonElement elementData, bool inventory, IMenu? prevMenu)
    {
        BaseMenu menu = CreateMenuByType(title);
        menu.ScreenMenu_ShowResolutionsOption = prevMenu == null;
        menu.PrevMenu = prevMenu;

        List<JsonProperty> items = GetElementJsonProperty(elementData);
        foreach (JsonProperty item in items)
        {
            if (item.Value.TryGetProperty("flag", out JsonElement flagElement) && !CheckFlag(player, flagElement.ToString(), true))
                continue;

            if (item.Value.TryGetProperty("uniqueid", out JsonElement uniqueIdElement))
            {
                menu.AddItems(player, uniqueIdElement, inventory, menu);
                continue;
            }

            if (inventory && !Item.PlayerHasAny(player, item.Value))
                continue;

            string categoryName = GetCategoryName(player, item);
            menu.AddItem(categoryName, (p, o) => OpenMenu(p, categoryName, item.Value, inventory, menu));
        }

        menu.Display(player, 0);
    }

    public static void AddItems(this IMenu menu, CCSPlayerController player, JsonElement uniqueIdElement, bool inventory, IMenu prevMenu)
    {
        if (!Instance.Items.TryGetValue(uniqueIdElement.ToString(), out Dictionary<string, string>? item))
            return;

        if (item.TryGetValue("enable", out string? enable) && enable != "true")
            return;

        if (!CheckFlag(player, item) || (inventory && !Item.PlayerHas(player, item["type"], item["uniqueid"], false)))
            return;

        if (Item.PlayerHas(player, item["type"], item["uniqueid"], false))
        {
            menu.AddMenuOption(player, (p, o) =>
            {
                p.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(p, item, inventory, prevMenu);
            }, Item.GetItemName(player, item));
        }
        else if (!inventory && !item.IsHidden())
        {
            menu.AddMenuOption(player, (p, o) => SelectPurchase(p, item, int.Parse(item["price"]) > 0, inventory, prevMenu),
                int.Parse(item["price"]) <= 0 ? "menu_store<purchase1>" : "menu_store<purchase>",
                Item.GetItemName(player, item), item["price"]);
        }
    }

    private static void SelectPurchase(CCSPlayerController player, Dictionary<string, string> item, bool confirm, bool inventory, IMenu prevMenu)
    {
        player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");

        if (confirm && Config.Menu.EnableConfirmMenu)
        {
            DisplayConfirmationMenu(player, item, inventory, prevMenu);
        }
        else if (Item.Purchase(player, item))
        {
            DisplayItemOption(player, item, inventory, prevMenu);
        }
        else
        {
            player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
        }
    }

    public static void DisplayItemOption(CCSPlayerController player, Dictionary<string, string> item, bool inventory, IMenu prevMenu)
    {
        BaseMenu menu = CreateMenuByType(Item.GetItemName(player, item));
        menu.PrevMenu = prevMenu;

        menu.AddInspectOption(player, item);

        if (Item.PlayerUsing(player, item["type"], item["uniqueid"]))
        {
            menu.AddMenuOption(player, (p, o) =>
            {
                if (Item.Unequip(p, item, true))
                {
                    p.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                    p.PrintToChatMessage("Purchase Unequip", Item.GetItemName(p, item));
                }
                DisplayItemOption(p, item, inventory, prevMenu);
            }, "menu_store<unequip>");
        }
        else
        {
            menu.AddMenuOption(player, (p, o) =>
            {
                if (Item.Equip(p, item))
                {
                    p.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                    p.PrintToChatMessage("Purchase Equip", Item.GetItemName(p, item));
                }
                DisplayItemOption(p, item, inventory, prevMenu);
            }, "menu_store<equip>");
        }

        StoreApi.Store.Store_Item? playerItem = Instance.GlobalStorePlayerItems.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == item["type"] && p.UniqueId == item["uniqueid"]);
        if (playerItem != null)
        {
            if (Config.Menu.EnableSelling && !Item.IsPlayerVip(player) && !CheckFlag(player, item, true))
            {
                int sellingPrice = GetSellingPrice(item, playerItem);
                if (sellingPrice > 1)
                {
                    menu.AddMenuOption(player, (p, o) =>
                    {
                        p.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                        Item.Sell(p, item);
                        p.PrintToChatMessage("Item Sell", Item.GetItemName(p, item));
                        DisplayStore(player, inventory);
                    }, "menu_store<sell>", sellingPrice);
                }
            }

            if (playerItem.DateOfExpiration > DateTime.MinValue)
                menu.AddItem(playerItem.DateOfExpiration.ToString(), DisableOption.DisableHideNumber);
        }

        menu.Display(player, 0);
    }

    public static void DisplayConfirmationMenu(CCSPlayerController player, Dictionary<string, string> item, bool inventory, IMenu prevMenu)
    {
        BaseMenu menu = CreateMenuByType(Instance.Localizer.ForPlayer(player, "menu_store<confirm_title>"));
        menu.PrevMenu = prevMenu;

        menu.AddMenuOption(player, DisableOption.DisableHideNumber, Item.GetItemName(player, item), item["price"]);
        menu.AddInspectOption(player, item);

        menu.AddMenuOption(player, (p, o) =>
        {
            if (Item.Purchase(p, item))
            {
                p.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(p, item, inventory, prevMenu);
            }
            else
            {
                p.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            }

            DisplayStore(player, inventory);
        }, "menu_store<yes>");

        menu.AddMenuOption(player, (p, o) =>
        {
            p.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            DisplayStore(p, inventory);
        }, "menu_store<no>");
        menu.Display(player, 0);
    }

    public static void AddInspectOption(this IMenu menu, CCSPlayerController player, Dictionary<string, string> item)
    {
        if (item["type"] is "playerskin" or "customweapon")
        {
            float waitTime = 0.0f;
            menu.AddMenuOption(player, (p, o) =>
            {
                if (Server.CurrentTime < waitTime) return;
                waitTime = Server.CurrentTime + 5.0f;
                InspectAction(p, item, item["type"]);
                menu.Display(player, 0);
            }, "menu_store<inspect>");
        }
    }
}
