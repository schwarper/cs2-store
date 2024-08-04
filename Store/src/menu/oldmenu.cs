using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Menu;
using System.Text;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class OldMenu
{
    public static void AddMenuOption(CCSPlayerController player, CenterHtmlMenu menu, Action<CCSPlayerController, ChatMenuOption> onSelect, bool disabled, string display, params object[] args)
    {
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            StringBuilder builder = new();
            builder.AppendFormat(Instance.Localizer[display, args]);

            menu.AddMenuOption(builder.ToString(), onSelect, disabled);
        }
    }

    public static void DisplayStore(CCSPlayerController player, bool inventory)
    {
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            StringBuilder builder = new();
            builder.AppendFormat(Instance.Localizer["menu_store<title>", Credits.Get(player)]);

            CenterHtmlMenu menu = new(builder.ToString(), Instance);

            foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> category in Instance.Config.Items)
            {
                if (inventory && !category.Value.Values.Any(item => Item.PlayerHas(player, item["type"], item["uniqueid"], false)))
                {
                    continue;
                }

                StringBuilder builderkey = new();
                builderkey.AppendFormat(Instance.Localizer[$"menu_store<{category.Key}>"]);

                menu.AddMenuOption(builderkey.ToString(), (CCSPlayerController player, ChatMenuOption option) =>
                {
                    player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundYes}");
                    DisplayItems(player, builderkey.ToString(), category.Value, inventory);
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

            foreach (int Slot in new[] { 1, 2, 3 })
            {
                if ((!playerSkinItems.Any(p => p.Value.TryGetValue("slot", out string? slot) && slot == Slot.ToString())) ||
                    (inventory && !playerSkinItems.Any(item => Item.PlayerHas(player, item.Value["type"], item.Value["uniqueid"], false))))
                {
                    continue;
                }

                using (new WithTemporaryCulture(player.GetLanguage()))
                {
                    StringBuilder builder = new();
                    builder.AppendFormat(Instance.Localizer[$"menu_store<{(Slot == 1 ? "all" : Slot == 2 ? "t" : "ct")}_title>"]);

                    menu.AddMenuOption(builder.ToString(), (CCSPlayerController player, ChatMenuOption option) =>
                    {
                        player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundYes}");
                        DisplayItem(player, inventory, builder.ToString(), playerSkinItems.Where(p => p.Value.TryGetValue("slot", out string? slot) && !string.IsNullOrEmpty(slot) && int.Parse(p.Value["slot"]) == Slot).ToDictionary(p => p.Key, p => p.Value));
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

            if (inventory && !Item.PlayerHas(player, item["type"], item["uniqueid"], false))
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
                }, false, item["name"]);
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

                }, false, "menu_store<purchase>", item["name"], item["price"]);
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
            }, false, "menu_store<unequip>");
        }
        else
        {
            AddMenuOption(player, menu, (player, option) =>
            {
                player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundYes}");
                Item.Equip(player, item);

                player.PrintToChatMessage("Purchase Equip", item["name"]);

                DisplayItemOption(player, item);
            }, false, "menu_store<equip>");
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
                }, false, "menu_store<sell>", sellingPrice);
            }
        }

        if (PlayerItems != null && PlayerItems.DateOfExpiration > DateTime.MinValue)
        {
            menu.AddMenuOption(PlayerItems.DateOfExpiration.ToString(), (p, o) => { }, true);
        }

        MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
    }

    public static void DisplayConfirmationMenu(CCSPlayerController player, Dictionary<string, string> item)
    {
        CenterHtmlMenu menu = new(Instance.Localizer["menu_store<confirm_title>"], Instance);

        AddMenuOption(player, menu, (p, o) => { }, true, "menu_store<confirm_item>", item["name"], item["price"]);

        AddMenuOption(player, menu, (p, o) =>
        {
            if (Item.Purchase(p, item))
            {
                player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(p, item);
            }
            else
            {
                MenuManager.CloseActiveMenu(player);
                player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundNo}");
            }

        }, false, "menu_store<yes>");

        AddMenuOption(player, menu, (p, o) =>
        {
            player.ExecuteClientCommand($"play {Instance.Config.Menu.MenuPressSoundNo}");
            MenuManager.CloseActiveMenu(player);
        }, false, "menu_store<no>");

        menu.Open(player);
    }
}