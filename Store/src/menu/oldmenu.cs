using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Menu;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class OldMenu
{
    public static void AddMenuOption(CCSPlayerController player, CenterHtmlMenu menu, Action<CCSPlayerController, ChatMenuOption> onSelect, string display, params object[] args)
    {
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            stringBuilder builder = new();
            builder.AppendFormat(Instance.Localizer[display, args]);

            menu.AddMenuOption(builder.Tostring(), onSelect);
        }
    }

    public static void DisplayStore(CCSPlayerController player, bool inventory)
    {
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            stringBuilder builder = new();
            builder.AppendFormat(Instance.Localizer["menu_store<title>", Credits.Get(player)]);

            CenterHtmlMenu menu = new(builder.Tostring(), Instance);

            foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> category in Instance.Config.Items)
            {
                if ((inventory || Item.IsPlayerVip(player)) && !category.Value.Values.Any(item => Item.PlayerHas(player, item["type"], item["uniqueid"], false)))
                {
                    continue;
                }

                stringBuilder builderkey = new();
                builderkey.AppendFormat(Instance.Localizer[$"menu_store<{category.Key}>"]);

                menu.AddMenuOption(builderkey.Tostring(), (CCSPlayerController player, ChatMenuOption option) =>
                {
                    player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundYes}");
                    DisplayItems(player, builderkey.Tostring(), category.Value, inventory);
                });
            }

            MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
        }
    }

    public static void DisplayItems(CCSPlayerController player, string key, Dictionary<string, Dictionary<string, string>> items, bool inventory)
    {
        Dictionary<string, Dictionary<string, string>> playerSkinItems = items.Where(p => p.Value["type"] == "playerskin" && p.Value["enable"] == "true").ToDictionary(p => p.Key, p => p.Value);

        if (playerSkinItems.Count != 0)
        {
            CenterHtmlMenu menu = new(key, Instance);

            foreach (int Slot in new[] { 2, 3 })
            {
                if ((inventory || Item.IsPlayerVip(player)) && !playerSkinItems.Any(item => Item.PlayerHas(player, item.Value["type"], item.Value["uniqueid"], false)))
                {
                    continue;
                }

                using (new WithTemporaryCulture(player.GetLanguage()))
                {
                    stringBuilder builder = new();
                    builder.AppendFormat(Instance.Localizer[$"menu_store<{(Slot == 2 ? "t" : "ct")}_title>"]);

                    menu.AddMenuOption(builder.Tostring(), (CCSPlayerController player, ChatMenuOption option) =>
                    {
                        player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundYes}");
                        DisplayItem(player, inventory, builder.Tostring(), playerSkinItems.Where(p => p.Value.TryGetValue("slot", out string? slot) && !string.IsNullOrEmpty(slot) && int.Parse(p.Value["slot"]) == Slot).ToDictionary(p => p.Key, p => p.Value));
                    });
                }
            }

            MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
        }
        else
        {
            DisplayItem(player, inventory, key, items);
        }
    }

    public static void DisplayItem(CCSPlayerController player, bool inventory, string key, Dictionary<string, Dictionary<string, string>> items)
    {
        CenterHtmlMenu menu = new(key, Instance);

        foreach (KeyValuePair<string, Dictionary<string, string>> kvp in items)
        {
            Dictionary<string, string> item = kvp.Value;

            if (item["enable"] != "true")
            {
                continue;
            }

            if ((inventory || Item.IsPlayerVip(player)) && !Item.PlayerHas(player, item["type"], item["uniqueid"], false))
            {
                continue;
            }

            bool isHidden = item.ContainsKey("hide") && item["hide"] == "true";
            if (Item.PlayerHas(player, item["type"], item["uniqueid"], false))
            {
                AddMenuOption(player, menu, (player, option) =>
                {
                    player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundYes}");
                    DisplayItemOption(player, item);
                }, item["name"]);
            }
            else if (!inventory && !isHidden)
            {
                AddMenuOption(player, menu, (player, option) =>
                {
                    if (Instance.Config.Menu.EnableConfirmMenu)
                    {
                        player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundYes}");
                        DisplayConfirmationMenu(player, item);
                    }
                    else
                    {
                        if (Item.Purchase(player, item))
                        {
                            player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundYes}");
                            DisplayItemOption(player, item);
                        }
                        else
                        {
                            player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundNo}");
                            WasdManager.CloseMenu(player);
                        }
                    }

                }, "menu_store<purchase>", item["name"], item["price"]);
            }
        }

        MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
    }

    public static void DisplayItemOption(CCSPlayerController player, Dictionary<string, string> item)
    {
        CenterHtmlMenu menu = new(item["name"], Instance);

        if (Item.PlayerUsing(player, item["type"], item["uniqueid"]))
        {
            AddMenuOption(player, menu, (player, option) =>
            {
                player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundYes}");
                Item.Unequip(player, item);

                player.PrintToChatMessage("Purchase Unequip", item["name"]);

                DisplayItemOption(player, item);
            }, "menu_store<unequip>");
        }
        else
        {
            AddMenuOption(player, menu, (player, option) =>
            {
                player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundYes}");
                Item.Equip(player, item);

                player.PrintToChatMessage("Purchase Equip", item["name"]);

                DisplayItemOption(player, item);
            }, "menu_store<equip>");
        }

        Store_Item? PlayerItems = Instance.GlobalStorePlayerItems.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == item["type"] && p.UniqueId == item["uniqueid"]);

        if (Instance.Config.Menu.EnableSelling && !Item.IsPlayerVip(player))
        {
            float sell_ratio = Instance.Config.Settings.SellRatio;

            int purchase_price = 1;

            bool usePurchaseCredit = Instance.Config.Settings.SellUsePurchaseCredit;

            if (usePurchaseCredit && PlayerItems != null)
            {
                purchase_price = PlayerItems.Price;
            }

            int sellingPrice = (int)((usePurchaseCredit ? purchase_price : int.Parse(item["price"])) * sell_ratio);

            if (sellingPrice > 1)
            {
                AddMenuOption(player, menu, (player, option) =>
                {
                    player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundYes}");
                    Item.Sell(player, item);

                    player.PrintToChatMessage("Item Sell", item["name"]);

                    MenuManager.CloseActiveMenu(player);
                }, "menu_store<sell>", sellingPrice);
            }
        }

        if (PlayerItems != null && PlayerItems.DateOfExpiration > DateTime.MinValue)
        {
            menu.AddMenuOption(PlayerItems.DateOfExpiration.Tostring(), (p, o) => { }, true);
        }

        MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
    }

    public static void DisplayConfirmationMenu(CCSPlayerController player, Dictionary<string, string> item)
    {
        CenterHtmlMenu menu = new(Instance.Localizer["menu_store<confirm_title>", item["name"], item["price"]], Instance);

        AddMenuOption(player, menu, (p, o) =>
        {
            if (Item.Purchase(p, item))
            {
                player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(p, item);
            }
            else
            {
                player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundNo}");
            }

        }, "menu_store<yes>");

        AddMenuOption(player, menu, (p, o) =>
        {
            player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundNo}");
            MenuManager.CloseActiveMenu(player);
        }, "menu_store<no>");

        menu.Open(player);
    }
}