using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
using CS2ScreenMenuAPI;
using CS2ScreenMenuAPI.Internal;
using System.Text;
using System.Text.Json;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Menu
{
    public static void AddMenuOption(CCSPlayerController player, ScreenMenu menu, Action<CCSPlayerController, IMenuOption> onSelect, bool disabled, string display, params object[] args)
    {
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            StringBuilder builder = new();
            builder.AppendFormat(Instance.Localizer[display, args]);

            menu.AddOption(builder.ToString(), onSelect, disabled);
        }
    }

    public static void DisplayStore(CCSPlayerController player, bool inventory)
    {
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            StringBuilder builder = new();
            builder.AppendFormat(Instance.Localizer["menu_store<title>", Credits.Get(player)]);

            OpenMenu(player, builder.ToString(), Instance.Config.Items, inventory, null);
        }
    }

    public static void OpenMenu(CCSPlayerController player, string title, JsonElement elementData, bool inventory, ScreenMenu? parent)
    {
        ScreenMenu menu = new(title, Instance)
        {
            ParentMenu = parent,
            IsSubMenu = parent != null
        };

        var items = elementData.EnumerateObject().Where(prop => prop.Name != "flag").ToList();

        foreach (var item in items)
        {
            if (item.Value.TryGetProperty("flag", out JsonElement flagElement) && !CheckFlag(player, flagElement.ToString(), true))
            {
                continue;
            }

            if (item.Value.TryGetProperty("uniqueid", out JsonElement uniqueIdElement))
            {
                AddItems(player, uniqueIdElement, inventory, menu);
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

            menu.AddOption(item.Name, (p, o) => OpenMenu(p, item.Name, item.Value, inventory, menu));
        }

        menu.Open(player);
    }

    private static void AddItems(CCSPlayerController player, JsonElement uniqueIdElement, bool inventory, ScreenMenu menu)
    {
        Dictionary<string, string> item = Instance.Items[uniqueIdElement.ToString()];

        if (item["enable"] != "true" || !CheckFlag(player, item))
        {
            return;
        }

        if (inventory && !Item.PlayerHas(player, item["type"], item["uniqueid"], false))
        {
            return;
        }

        if (Item.PlayerHas(player, item["type"], item["uniqueid"], false))
        {
            AddMenuOption(player, menu, (player, option) =>
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(player, item, menu);
            }, false, item["name"]);
        }
        else if (!inventory && !item.IsHidden())
        {
            if (int.Parse(item["price"]) <= 0)
            {
                AddMenuOption(player, menu, (player, option) => SelectPurchase(player, item, false, menu), false, "menu_store<purchase1>", item["name"]);
            }
            else
            {
                AddMenuOption(player, menu, (player, option) => SelectPurchase(player, item, true, menu), false, "menu_store<purchase>", item["name"], item["price"]);
            }
        }
    }

    private static void SelectPurchase(CCSPlayerController player, Dictionary<string, string> item, bool confirm, ScreenMenu parent)
    {
        if (confirm && Config.Menu.EnableConfirmMenu)
        {
            player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
            DisplayConfirmationMenu(player, item, parent);
        }
        else
        {
            if (Item.Purchase(player, item))
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(player, item, parent);
            }
            else
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            }
        }
    }

    public static void DisplayItemOption(CCSPlayerController player, Dictionary<string, string> item, ScreenMenu parent)
    {
        ScreenMenu menu = new(item["name"], Instance)
        {
            ParentMenu = parent,
            IsSubMenu = true
        };

        if (Item.PlayerUsing(player, item["type"], item["uniqueid"]))
        {
            AddMenuOption(player, menu, (player, option) =>
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                Item.Unequip(player, item, true);

                player.PrintToChatMessage("Purchase Unequip", item["name"]);

                DisplayItemOption(player, item, menu);
            }, false, "menu_store<unequip>");
        }
        else
        {
            AddMenuOption(player, menu, (player, option) =>
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                Item.Equip(player, item);

                player.PrintToChatMessage("Purchase Equip", item["name"]);

                DisplayItemOption(player, item, menu);
            }, false, "menu_store<equip>");
        }

        Store_Item? PlayerItems = Instance.GlobalStorePlayerItems.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == item["type"] && p.UniqueId == item["uniqueid"]);

        if (Config.Menu.EnableSelling && !Item.IsPlayerVip(player))
        {
            float sell_ratio = Config.Settings.SellRatio;

            int purchase_price = 1;

            bool usePurchaseCredit = Config.Settings.SellUsePurchaseCredit;

            if (usePurchaseCredit && PlayerItems != null)
            {
                purchase_price = PlayerItems.Price;
            }

            int sellingPrice = (int)((usePurchaseCredit ? purchase_price : int.Parse(item["price"])) * sell_ratio);

            if (sellingPrice > 1)
            {
                AddMenuOption(player, menu, (player, option) =>
                {
                    player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                    Item.Sell(player, item);

                    player.PrintToChatMessage("Item Sell", item["name"]);

                    MenuManager.CloseActiveMenu(player);
                }, false, "menu_store<sell>", sellingPrice);
            }
        }

        if (PlayerItems != null && PlayerItems.DateOfExpiration > DateTime.MinValue)
        {
            menu.AddOption(PlayerItems.DateOfExpiration.ToString(), (p, o) => { }, true);
        }

        menu.Open(player);
    }

    public static void DisplayConfirmationMenu(CCSPlayerController player, Dictionary<string, string> item, ScreenMenu parent)
    {
        ScreenMenu menu = new(Instance.Localizer["menu_store<confirm_title>"], Instance)
        {
            ParentMenu = parent,
            IsSubMenu = true
        };

        AddMenuOption(player, menu, (p, o) => { }, true, "menu_store<confirm_item>", item["name"], item["price"]);

        AddMenuOption(player, menu, (p, o) =>
        {
            if (Item.Purchase(p, item))
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(p, item, menu);
            }
            else
            {
                MenuManager.CloseActiveMenu(player);
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            }

        }, false, "menu_store<yes>");

        AddMenuOption(player, menu, (p, o) =>
        {
            player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            MenuManager.CloseActiveMenu(player);
        }, false, "menu_store<no>");

        menu.Open(player);
    }

    public static bool CheckFlag(CCSPlayerController player, Dictionary<string, string> item)
    {
        item.TryGetValue("flag", out string? flag);

        return CheckFlag(player, flag, true);
    }

    public static bool CheckFlag(CCSPlayerController player, string? flagAll, bool trueifNull)
    {
        if (string.IsNullOrEmpty(flagAll))
        {
            return trueifNull;
        }

        var flags = flagAll.Split(',');

        foreach (var flag in flags)
        {
            if (flag.StartsWith('@') && AdminManager.PlayerHasPermissions(player, flag))
            {
                return true;
            }
            else if (flag.StartsWith('#') && AdminManager.PlayerInGroup(player, flag))
            {
                return true;
            }
            else if (flag == player.SteamID.ToString())
            {
                return true;
            }
        }

        return false;
    }
}