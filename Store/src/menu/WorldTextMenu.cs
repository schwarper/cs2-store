using System.Text;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CS2ScreenMenuAPI;
using CS2ScreenMenuAPI.Interfaces;
using CS2ScreenMenuAPI.Internal;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class WorldTextMenu
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

            ScreenMenu menu = new(builder.ToString(), Instance)
            {
                IsSubMenu = false,
            };

            foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> category in Instance.Config.Items)
            {
                if (inventory && !category.Value.Values.Any(item => Item.PlayerHas(player, item["type"], item["uniqueid"], false)))
                {
                    continue;
                }

                StringBuilder builderkey = new();
                builderkey.AppendFormat(Instance.Localizer[$"menu_store<{category.Key}>"]);

                menu.AddOption(builderkey.ToString(), (CCSPlayerController player, IMenuOption option) =>
                {
                    player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                    DisplayItems(player, builderkey.ToString(), category.Value, inventory, menu);
                });
            }

            MenuAPI.OpenMenu(Instance, player, menu);
        }
    }

    public static void DisplayItems(CCSPlayerController player, string key, Dictionary<string, Dictionary<string, string>> items, bool inventory, ScreenMenu parentMenu)
    {
        Dictionary<string, Dictionary<string, string>> playerSkinItems = items.Where(p => p.Value["type"] == "playerskin" && p.Value["enable"] == "true").ToDictionary(p => p.Key, p => p.Value);

        if (playerSkinItems.Count != 0)
        {
            ScreenMenu menu = new(key, Instance)
            {
                IsSubMenu = true,
                ParentMenu = parentMenu
            };

            foreach (int Slot in new[] { 1, 2, 3 })
            {
                if (!Menu.IsAnyItemExistInPlayerSkins(player, Slot, inventory, playerSkinItems))
                {
                    continue;
                }

                using (new WithTemporaryCulture(player.GetLanguage()))
                {
                    StringBuilder builder = new();
                    builder.AppendFormat(Instance.Localizer[$"menu_store<{(Slot == 1 ? "all" : Slot == 2 ? "t" : "ct")}_title>"]);

                    menu.AddOption(builder.ToString(), (CCSPlayerController player, IMenuOption option) =>
                    {
                        player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                        DisplayItem(player, inventory, builder.ToString(), playerSkinItems.Where(p => p.Value.TryGetValue("slot", out string? slot) && !string.IsNullOrEmpty(slot) && int.Parse(p.Value["slot"]) == Slot).ToDictionary(p => p.Key, p => p.Value), menu);
                    });
                }
            }
            MenuAPI.OpenSubMenu(Instance, player, menu);
        }
        else
        {
            DisplayItem(player, inventory, key, items, parentMenu);
        }
    }

    public static void DisplayItem(CCSPlayerController player, bool inventory, string key, Dictionary<string, Dictionary<string, string>> items, ScreenMenu parentMenu)
    {
        ScreenMenu menu = new(key, Instance)
        {
            IsSubMenu = true,
            ParentMenu = parentMenu
        };

        foreach (KeyValuePair<string, Dictionary<string, string>> kvp in items)
        {
            Dictionary<string, string> item = kvp.Value;

            if (item["enable"] != "true" || !Menu.CheckFlag(player, item))
            {
                continue;
            }

            if (inventory && !Item.PlayerHas(player, item["type"], item["uniqueid"], false))
            {
                continue;
            }

            if (Item.PlayerHas(player, item["type"], item["uniqueid"], false))
            {
                AddMenuOption(player, menu, (player, option) =>
                {
                    player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                    DisplayItemOption(player, item, parentMenu);
                }, false, item["name"]);
            }
            else if (!inventory && !item.IsHidden())
            {
                if (int.Parse(item["price"]) <= 0)
                {
                    AddMenuOption(player, menu, (player, option) => SelectPurchase(player, item, false, parentMenu), false, "menu_store<purchase1>", item["name"]);
                }
                else
                {
                    AddMenuOption(player, menu, (player, option) => SelectPurchase(player, item, true, parentMenu), false, "menu_store<purchase>", item["name"], item["price"]);
                }
            }
        }

        MenuAPI.OpenSubMenu(Instance, player, menu);
    }

    private static void SelectPurchase(CCSPlayerController player, Dictionary<string, string> item, bool confirm, ScreenMenu parentMenu)
    {
        if (confirm && Config.Menu.EnableConfirmMenu)
        {
            player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
            DisplayConfirmationMenu(player, item, parentMenu);
        }
        else
        {
            if (Item.Purchase(player, item))
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(player, item, parentMenu);
            }
            else
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
                MenuAPI.CloseActiveMenu(player);
            }
        }
    }

    public static void DisplayItemOption(CCSPlayerController player, Dictionary<string, string> item, ScreenMenu parentMenu)
    {
        ScreenMenu menu = new(item["name"], Instance)
        {
            IsSubMenu = true,
            ParentMenu = parentMenu
        };

        if (Item.PlayerUsing(player, item["type"], item["uniqueid"]))
        {
            AddMenuOption(player, menu, (player, option) =>
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                Item.Unequip(player, item, true);

                player.PrintToChatMessage("Purchase Unequip", item["name"]);

                DisplayItemOption(player, item, parentMenu);
            }, false, "menu_store<unequip>");
        }
        else
        {
            AddMenuOption(player, menu, (player, option) =>
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                Item.Equip(player, item);

                player.PrintToChatMessage("Purchase Equip", item["name"]);

                DisplayItemOption(player, item, parentMenu);
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

                    MenuAPI.CloseActiveMenu(player);
                }, false, "menu_store<sell>", sellingPrice);
            }
        }

        if (PlayerItems != null && PlayerItems.DateOfExpiration > DateTime.MinValue)
        {
            menu.AddOption(PlayerItems.DateOfExpiration.ToString(), (p, o) => { }, true);
        }
        MenuAPI.OpenSubMenu(Instance, player, menu);
    }

    public static void DisplayConfirmationMenu(CCSPlayerController player, Dictionary<string, string> item, ScreenMenu parentMenu)
    {
        ScreenMenu menu = new(Instance.Localizer["menu_store<confirm_title>"], Instance)
        {
            IsSubMenu = true,
            ParentMenu = parentMenu
        };

        AddMenuOption(player, menu, (p, o) => { }, true, "menu_store<confirm_item>", item["name"], item["price"]);

        AddMenuOption(player, menu, (p, o) =>
        {
            if (Item.Purchase(p, item))
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(p, item, parentMenu);
            }
            else
            {
                MenuAPI.CloseActiveMenu(player);
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            }

        }, false, "menu_store<yes>");

        AddMenuOption(player, menu, (p, o) =>
        {
            player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            MenuAPI.CloseActiveMenu(player);
        }, false, "menu_store<no>");

        MenuAPI.OpenSubMenu(Instance, player, menu);
    }
}