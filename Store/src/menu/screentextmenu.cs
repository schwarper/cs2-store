using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CS2ScreenMenuAPI;
using CS2ScreenMenuAPI.Internal;
using System.Text.Json;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class ScreenTextMenu
{
    public static void AddMenuOption(this ScreenMenu menu, CCSPlayerController player, Action<CCSPlayerController, IMenuOption> callback, bool disabled, string display, params object[] args) =>
        menu.AddOption(Instance.Localizer.ForPlayer(player, display, args), callback, disabled);

    public static void DisplayStore(CCSPlayerController player, bool inventory) =>
        OpenMenu(player, Instance.Localizer.ForPlayer(player, "menu_store<title>", Credits.Get(player)), Instance.Config.Items, inventory);

    public static void OpenMenu(CCSPlayerController player, string title, JsonElement elementData, bool inventory, ScreenMenu? mainMenu = null, ScreenMenu? parentMenu = null)
    {
        ScreenMenu menu = new(title, Instance)
        {
            ParentMenu = parentMenu,
            IsSubMenu = true
        };

        mainMenu ??= menu;

        List<JsonProperty> items = [.. elementData.EnumerateObject().Where(prop => prop.Name != "flag" && prop.Name != "langname")];

        foreach (JsonProperty item in items)
        {
            if (item.Value.TryGetProperty("flag", out JsonElement flagElement) && !Menu.CheckFlag(player, flagElement.ToString(), true))
            {
                continue;
            }

            if (item.Value.TryGetProperty("uniqueid", out JsonElement uniqueIdElement))
            {
                menu.AddItems(player, uniqueIdElement, inventory, mainMenu, menu);
                continue;
            }

            if (inventory)
            {
                Dictionary<string, Dictionary<string, string>> itemsDictionary = [];
                ExtractItems(item.Value, itemsDictionary);

                if (!itemsDictionary.Values.Any(item => Item.PlayerHas(player, item["type"], item["uniqueid"], false)))
                {
                    continue;
                }
            }

            var categoryName = Menu.GetCategoryName(player, item);

            menu.AddOption(categoryName, (p, o) => OpenMenu(p, categoryName, item.Value, inventory, mainMenu, menu));
        }

        if (menu.IsSubMenu)
        {
            MenuAPI.OpenSubMenu(Instance, player, menu);
        }
        else
        {
            MenuAPI.OpenMenu(Instance, player, menu);
        }
    }

    public static void AddItems(this ScreenMenu menu, CCSPlayerController player, JsonElement uniqueIdElement, bool inventory, ScreenMenu mainMenu, ScreenMenu parentMenu)
    {
        Dictionary<string, string> item = Instance.Items[uniqueIdElement.ToString()];

        if (item["enable"] != "true" || !Menu.CheckFlag(player, item))
        {
            return;
        }

        if (inventory && !Item.PlayerHas(player, item["type"], item["uniqueid"], false))
        {
            return;
        }

        if (Item.PlayerHas(player, item["type"], item["uniqueid"], false))
        {
            menu.AddMenuOption(player, (player, option) =>
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(player, item, mainMenu, parentMenu);
            }, false, Item.GetItemName(player, item));
        }
        else if (!inventory && !item.IsHidden())
        {
            if (int.Parse(item["price"]) <= 0)
            {
                menu.AddMenuOption(player, (player, option) => SelectPurchase(player, item, false, mainMenu), false, "menu_store<purchase1>", Item.GetItemName(player, item));
            }
            else
            {
                menu.AddMenuOption(player, (player, option) => SelectPurchase(player, item, true, mainMenu), false, "menu_store<purchase>", Item.GetItemName(player, item), item["price"]);
            }
        }
    }

    private static void SelectPurchase(CCSPlayerController player, Dictionary<string, string> item, bool confirm, ScreenMenu mainMenu)
    {
        if (confirm && Config.Menu.EnableConfirmMenu)
        {
            player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
            DisplayConfirmationMenu(player, item, mainMenu, mainMenu);
        }
        else
        {
            if (Item.Purchase(player, item))
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(player, item, mainMenu, mainMenu);
            }
            else
            {
                if (item["price"] == "0")
                {
                    Store.Api.PlayerPurchaseItem(player, item);
                    player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                    player.PrintToChatMessage("Purchase Succeeded", Item.GetItemName(player, item));
                }
                else
                {
                    player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
                }
            }
        }
    }

    public static void DisplayItemOption(CCSPlayerController player, Dictionary<string, string> item, ScreenMenu mainMenu, ScreenMenu parentMenu)
    {
        ScreenMenu menu = new(Item.GetItemName(player, item), Instance)
        {
            ParentMenu = parentMenu,
            IsSubMenu = true
        };

        menu.AddInspectOption(player, item);

        if (Item.PlayerUsing(player, item["type"], item["uniqueid"]))
        {
            menu.AddMenuOption(player, (player, option) =>
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                Item.Unequip(player, item, true);

                player.PrintToChatMessage("Purchase Unequip", Item.GetItemName(player, item));

                DisplayItemOption(player, item, mainMenu, parentMenu);
            }, false, "menu_store<unequip>");
        }
        else
        {
            menu.AddMenuOption(player, (player, option) =>
            {
                if (Item.Equip(player, item))
                {
                    player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                    player.PrintToChatMessage("Purchase Equip", Item.GetItemName(player, item));
                }

                DisplayItemOption(player, item, mainMenu, parentMenu);
            }, false, "menu_store<equip>");
        }

        Store_Item? playerItem = Instance.GlobalStorePlayerItems.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == item["type"] && p.UniqueId == item["uniqueid"]);

        if (playerItem != null)
        {
            if (Config.Menu.EnableSelling && !Item.IsPlayerVip(player) && !Menu.CheckFlag(player, item, true))
            {
                int sellingPrice = Menu.GetSellingPrice(item, playerItem);

                if (sellingPrice > 1)
                {
                    menu.AddMenuOption(player, (player, option) =>
                    {
                        player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                        Item.Sell(player, item);
                        player.PrintToChatMessage("Item Sell", Item.GetItemName(player, item));

                        MenuAPI.OpenMenu(Instance, player, mainMenu);
                    }, false, "menu_store<sell>", sellingPrice);
                }
            }

            if (playerItem.DateOfExpiration > DateTime.MinValue)
            {
                menu.AddOption(playerItem.DateOfExpiration.ToString(), (p, o) => { }, true);
            }
        }

        MenuAPI.OpenSubMenu(Instance, player, menu);
    }

    public static void DisplayConfirmationMenu(CCSPlayerController player, Dictionary<string, string> item, ScreenMenu mainMenu, ScreenMenu parentMenu)
    {
        ScreenMenu menu = new(Instance.Localizer.ForPlayer(player, "menu_store<confirm_title>"), Instance)
        {
            ParentMenu = parentMenu,
            IsSubMenu = true
        };

        menu.AddMenuOption(player, (p, o) => { }, true, "menu_store<confirm_item>", Item.GetItemName(player, item), item["price"]);

        menu.AddInspectOption(player, item);

        menu.AddMenuOption(player, (p, o) =>
        {
            if (Item.Purchase(p, item))
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(p, item, mainMenu, parentMenu);
            }
            else
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            }

        }, false, "menu_store<yes>");

        menu.AddMenuOption(player, (p, o) =>
        {
            player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            MenuAPI.OpenSubMenu(Instance, player, parentMenu);
        }, false, "menu_store<no>");

        MenuAPI.OpenSubMenu(Instance, player, menu);
    }

    public static void AddInspectOption(this ScreenMenu menu, CCSPlayerController player, Dictionary<string, string> item)
    {
        if (item["type"] == "playerskin" || item["type"] == "customweapon")
        {
            float waitTime = 0.0f;

            Dictionary<string, Action> inspectActions = new Dictionary<string, Action>
            {
                { "playerskin", () => Item_PlayerSkin.Inspect(player, item["uniqueid"]) },
                { "customweapon", () => Item_CustomWeapon.Inspect(player, item["uniqueid"], item["weapon"]) }
            };

            menu.AddMenuOption(player, (p, o) =>
            {
                if (Server.CurrentTime < waitTime)
                    return;

                waitTime = Server.CurrentTime + 5.0f;

                if (inspectActions.TryGetValue(item["type"], out Action? action))
                {
                    action();
                }
            }, false, "menu_store<inspect>");
        }
    }
}