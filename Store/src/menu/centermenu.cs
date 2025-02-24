using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Menu;
using System.Text.Json;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class CenterMenu
{
    public static void AddMenuOption(this CenterHtmlMenu menu, CCSPlayerController player, Action<CCSPlayerController, ChatMenuOption> callback, bool disabled, string display, params object[] args) =>
        menu.AddMenuOption(Instance.Localizer.ForPlayer(player, display, args), callback, disabled);

    public static void DisplayStore(CCSPlayerController player, bool inventory) =>
        OpenMenu(player, Instance.Localizer.ForPlayer(player, "menu_store<title>", Credits.Get(player)), Instance.Config.Items, inventory);

    public static void OpenMenu(CCSPlayerController player, string title, JsonElement elementData, bool inventory)
    {
        CenterHtmlMenu menu = new(title, Instance);

        var items = elementData.EnumerateObject().Where(prop => prop.Name != "flag").ToList();

        foreach (var item in items)
        {
            if (item.Value.TryGetProperty("flag", out JsonElement flagElement) && !Menu.CheckFlag(player, flagElement.ToString(), true))
            {
                continue;
            }

            if (item.Value.TryGetProperty("uniqueid", out JsonElement uniqueIdElement))
            {
                menu.AddItems(player, uniqueIdElement, inventory);
                continue;
            }

            if (inventory)
            {
                var itemsDictionary = new Dictionary<string, Dictionary<string, string>>();
                ExtractItems(item.Value, itemsDictionary);

                if (!itemsDictionary.Values.Any(item => Item.PlayerHas(player, item["type"], item["uniqueid"], false)))
                {
                    continue;
                }
            }

            menu.AddMenuOption(item.Name, (p, o) => OpenMenu(p, item.Name, item.Value, inventory));
        }

        menu.Open(player);
    }

    public static void AddItems(this CenterHtmlMenu menu, CCSPlayerController player, JsonElement uniqueIdElement, bool inventory)
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
                DisplayItemOption(player, item);
            }, false, item["name"]);
        }
        else if (!inventory && !item.IsHidden())
        {
            if (int.Parse(item["price"]) <= 0)
            {
                menu.AddMenuOption(player, (player, option) => SelectPurchase(player, item, false), false, "menu_store<purchase1>", item["name"]);
            }
            else
            {
                menu.AddMenuOption(player, (player, option) => SelectPurchase(player, item, true), false, "menu_store<purchase>", item["name"], item["price"]);
            }
        }
    }

    private static void SelectPurchase(CCSPlayerController player, Dictionary<string, string> item, bool confirm)
    {
        if (confirm && Config.Menu.EnableConfirmMenu)
        {
            player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
            DisplayConfirmationMenu(player, item);
        }
        else
        {
            if (Item.Purchase(player, item))
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(player, item);
            }
            else
            {
                if (item["price"] == "0")
                {
                    Store.Api.PlayerPurchaseItem(player, item);
                    player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                    player.PrintToChatMessage("Purchase Succeeded", item["name"]);
                }
                else
                {
                    player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
                }
            }
        }
    }

    public static void DisplayItemOption(CCSPlayerController player, Dictionary<string, string> item)
    {
        CenterHtmlMenu menu = new(item["name"], Instance);

        if (Item.PlayerUsing(player, item["type"], item["uniqueid"]))
        {
            menu.AddMenuOption(player, (player, option) =>
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                Item.Unequip(player, item, true);

                player.PrintToChatMessage("Purchase Unequip", item["name"]);

                DisplayItemOption(player, item);
            }, false, "menu_store<unequip>");
        }
        else
        {
            menu.AddMenuOption(player, (player, option) =>
            {
                if (Item.Equip(player, item))
                {
                    player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                    player.PrintToChatMessage("Purchase Equip", item["name"]);
                }

                DisplayItemOption(player, item);
            }, false, "menu_store<equip>");
        }

        Store_Item? playerItem = Instance.GlobalStorePlayerItems.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == item["type"] && p.UniqueId == item["uniqueid"]);

        if (playerItem == null)
        {
            return;
        }

        if (Config.Menu.EnableSelling && !Item.IsPlayerVip(player) && !Menu.CheckFlag(player, item))
        {
            int sellingPrice = Menu.GetSellingPrice(item, playerItem);

            if (sellingPrice > 1)
            {
                menu.AddMenuOption(player, (player, option) =>
                {
                    player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                    Item.Sell(player, item);
                    player.PrintToChatMessage("Item Sell", item["name"]);
                    MenuManager.CloseActiveMenu(player);
                }, false, "menu_store<sell>", sellingPrice);
            }
        }

        if (playerItem != null && playerItem.DateOfExpiration > DateTime.MinValue)
        {
            menu.AddMenuOption(playerItem.DateOfExpiration.ToString(), (p, o) => { }, true);
        }

        menu.Open(player);
    }

    public static void DisplayConfirmationMenu(CCSPlayerController player, Dictionary<string, string> item)
    {
        CenterHtmlMenu menu = new(Instance.Localizer.ForPlayer(player, "menu_store<confirm_title>"), Instance);

        menu.AddMenuOption(player, (p, o) => { }, true, "menu_store<confirm_item>", item["name"], item["price"]);

        if (item["type"] == "playerskin")
        {
            float waitTime = 0.0f;

            menu.AddMenuOption(player, (p, o) =>
            {
                var currentTime = Server.CurrentTime;

                if (waitTime - currentTime > 0)
                {
                    return;
                }

                waitTime = currentTime + 5.0f;

                Item_PlayerSkin.InspectPlayerSkin(player, item["uniqueid"]);
            }, false, "menu_store<inspect>");
        }

        menu.AddMenuOption(player, (p, o) =>
        {
            if (Item.Purchase(p, item))
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(p, item);
            }
            else
            {
                MenuManager.CloseActiveMenu(player);
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            }

        }, false, "menu_store<yes>");

        menu.AddMenuOption(player, (p, o) =>
        {
            player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            MenuManager.CloseActiveMenu(player);
        }, false, "menu_store<no>");

        menu.Open(player);
    }
}