using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using System.Text;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Menu
{
    public static readonly Dictionary<int, WasdMenuPlayer> Players = [];

    public static void SetSettings(bool hotReload)
    {
        Instance.RegisterEventHandler<EventPlayerActivate>((@event, info) =>
        {
            CCSPlayerController? player = @event.Userid;

            if (player == null)
            {
                return HookResult.Continue;
            }

            Players[player.Slot] = new WasdMenuPlayer
            {
                player = player,
                Buttons = 0
            };

            return HookResult.Continue;
        });

        Instance.RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            CCSPlayerController? player = @event.Userid;

            if (player == null)
            {
                return HookResult.Continue;
            }

            Players.Remove(player.Slot);

            return HookResult.Continue;
        });

        if (hotReload)
        {
            foreach (CCSPlayerController pl in Utilities.GetPlayers())
            {
                Players[pl.Slot] = new WasdMenuPlayer
                {
                    player = pl,
                    Buttons = pl.Buttons
                };
            }
        }
    }

    public static void AddMenuOption(CCSPlayerController player, IWasdMenu menu, Action<CCSPlayerController, IWasdMenuOption> onSelect, string display, params object[] args)
    {
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            StringBuilder builder = new();
            builder.AppendFormat(Instance.Localizer[display, args]);

            menu.Add(builder.ToString(), onSelect);
        }
    }

    public static void DisplayStore(CCSPlayerController player, bool inventory)
    {
        if (!Config.Menu.UseWASDMenu)
        {
            OldMenu.DisplayStore(player, inventory);
            return;
        }

        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            StringBuilder builder = new();
            builder.AppendFormat(Instance.Localizer["menu_store<title>", Credits.Get(player)]);

            IWasdMenu menu = WasdManager.CreateMenu(builder.ToString());

            foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> category in Instance.Config.Items)
            {
                if (inventory && !category.Value.Values.Any(item => Item.PlayerHas(player, item["type"], item["uniqueid"], false)))
                {
                    continue;
                }

                StringBuilder builderkey = new();
                builderkey.AppendFormat(Instance.Localizer[$"menu_store<{category.Key}>"]);

                menu.Add(builderkey.ToString(), (CCSPlayerController player, IWasdMenuOption option) =>
                {
                    player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                    DisplayItems(player, builderkey.ToString(), category.Value, inventory, option);
                });
            }

            WasdManager.OpenMainMenu(player, menu);
        }
    }

    public static void DisplayItems(CCSPlayerController player, string key, Dictionary<string, Dictionary<string, string>> items, bool inventory, IWasdMenuOption? prev = null)
    {
        Dictionary<string, Dictionary<string, string>> playerSkinItems = items.Where(p => p.Value["type"] == "playerskin" && p.Value["enable"] == "true").ToDictionary(p => p.Key, p => p.Value);

        if (playerSkinItems.Count != 0)
        {
            IWasdMenu menu = WasdManager.CreateMenu(key);
            if (prev != null)
                menu.Prev = prev.Parent?.Options?.Find(prev);

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

                    menu.Add(builder.ToString(), (CCSPlayerController player, IWasdMenuOption option) =>
                    {
                        player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                        DisplayItem(player, inventory, builder.ToString(), playerSkinItems.Where(p => p.Value.TryGetValue("slot", out string? slot) && !string.IsNullOrEmpty(slot) && int.Parse(p.Value["slot"]) == Slot).ToDictionary(p => p.Key, p => p.Value), option);
                    });
                }
            }

            WasdManager.OpenSubMenu(player, menu);
        }
        else
        {
            DisplayItem(player, inventory, key, items, prev);
        }
    }

    public static void DisplayItem(CCSPlayerController player, bool inventory, string key, Dictionary<string, Dictionary<string, string>> items, IWasdMenuOption? prev = null)
    {
        IWasdMenu menu = WasdManager.CreateMenu(key);
        if (prev != null)
            menu.Prev = prev.Parent?.Options?.Find(prev);

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
                    player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                    DisplayItemOption(player, item, option);
                }, item["name"]);
            }
            else if (!inventory && !isHidden)
            {
                AddMenuOption(player, menu, (player, option) =>
                {
                    if (Config.Menu.EnableConfirmMenu)
                    {
                        player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                        DisplayConfirmationMenu(player, item, option);
                    }
                    else
                    {
                        if (Item.Purchase(player, item))
                        {
                            player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                            DisplayItemOption(player, item, option);
                        }
                        else
                        {
                            player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
                            WasdManager.CloseMenu(player);
                        }
                    }

                }, "menu_store<purchase>", item["name"], item["price"]);
            }
        }

        WasdManager.OpenSubMenu(player, menu);
    }

    public static void DisplayItemOption(CCSPlayerController player, Dictionary<string, string> item, IWasdMenuOption? prev = null)
    {
        IWasdMenu menu = WasdManager.CreateMenu(item["name"]);
        if (prev != null)
            menu.Prev = prev.Parent?.Options?.Find(prev);

        if (Item.PlayerUsing(player, item["type"], item["uniqueid"]))
        {
            AddMenuOption(player, menu, (player, option) =>
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                Item.Unequip(player, item, true);

                player.PrintToChatMessage("Purchase Unequip", item["name"]);

                DisplayItemOption(player, item, prev);
            }, "menu_store<unequip>");
        }
        else
        {
            AddMenuOption(player, menu, (player, option) =>
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                Item.Equip(player, item);

                player.PrintToChatMessage("Purchase Equip", item["name"]);

                DisplayItemOption(player, item, prev);
            }, "menu_store<equip>");
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

                    WasdManager.CloseMenu(player);
                }, "menu_store<sell>", sellingPrice);
            }
        }

        if (PlayerItems != null && PlayerItems.DateOfExpiration > DateTime.MinValue)
        {
            menu.Add(PlayerItems.DateOfExpiration.ToString(), (p, o) => { DisplayItemOption(player, item); });
        }

        WasdManager.OpenSubMenu(player, menu);
    }

    public static void DisplayConfirmationMenu(CCSPlayerController player, Dictionary<string, string> item, IWasdMenuOption? prev = null)
    {
        IWasdMenu menu = WasdManager.CreateMenu(Instance.Localizer["menu_store<confirm_title>", item["name"], item["price"]]);
        if (prev != null)
            menu.Prev = prev.Parent?.Options?.Find(prev);

        AddMenuOption(player, menu, (p, o) => { }, "menu_store<confirm_item>", item["name"], item["price"]);

        AddMenuOption(player, menu, (p, o) =>
        {
            if (Item.Purchase(p, item))
            {
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(p, item, o);
            }
            else
            {
                WasdManager.CloseSubMenu(p);
                player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            }
        }, "menu_store<yes>");

        AddMenuOption(player, menu, (p, o) =>
        {
            player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            WasdManager.CloseSubMenu(p);
        }, "menu_store<no>");

        WasdManager.OpenSubMenu(player, menu);
    }

    public static void OnTick()
    {
        foreach (WasdMenuPlayer? player in Players.Values.Where(p => p.MainMenu != null))
        {
            if ((player.Buttons & PlayerButtons.Forward) == 0 && (player.player.Buttons & PlayerButtons.Forward) != 0)
            {
                player.ScrollUp();
            }
            else if ((player.Buttons & PlayerButtons.Back) == 0 && (player.player.Buttons & PlayerButtons.Back) != 0)
            {
                player.ScrollDown();
            }
            else if ((player.Buttons & PlayerButtons.Moveright) == 0 && (player.player.Buttons & PlayerButtons.Moveright) != 0)
            {
                player.Choose();
            }
            else if ((player.Buttons & PlayerButtons.Moveleft) == 0 && (player.player.Buttons & PlayerButtons.Moveleft) != 0)
            {
                player.CloseSubMenu();
            }

            if (((long)player.player.Buttons & 8589934592) == 8589934592)
            {
                player.OpenMainMenu(null);
            }

            player.Buttons = player.player.Buttons;

            if (player.CenterHtml != "")
            {
                Server.NextFrame(() =>
                    player.player.PrintToCenterHtml(player.CenterHtml)
                );
            }
        }
    }
}
