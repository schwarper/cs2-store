using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Menu;
using System.Text;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Menu
{
    public static void DisplayStore(CCSPlayerController player, bool inventory)
    {
        var isVip = new StoreAPI().IsPlayerVip(player);

        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            StringBuilder builder = new();
            builder.AppendFormat(Instance.Localizer["menu_store<title>"]);

            CenterHtmlMenu menu = new(builder.ToString());

            foreach (KeyValuePair<string, Dictionary<string, Store_Item>> category in Instance.Config.Items)
            {
                if (inventory && !category.Value.Values.Any(p => Item.PlayerHas(player, p.UniqueId) || isVip && p.Slot != 0))
                {
                    continue;
                }

                StringBuilder builderkey = new();
                builderkey.AppendFormat(Instance.Localizer[$"menu_store<{category.Key}>"]);

                menu.AddMenuOption(builderkey.ToString(), (CCSPlayerController player, ChatMenuOption option) =>
                {
                    DisplayItems(player, builderkey.ToString(), category.Value, inventory, isVip);
                });
            }

            MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
        }
    }

    public static void DisplayItems(CCSPlayerController player, string key, Dictionary<string, Store_Item> items, bool inventory, bool isVip)
    {
        Dictionary<string, Store_Item> playerSkinItems = items.Where(p => p.Value.Type == "playerskin" && p.Value.Enable).ToDictionary(p => p.Key, p => p.Value);

        if (playerSkinItems.Any())
        {
            CenterHtmlMenu menu = new(key);

            foreach (int Slot in new[] { 2, 3 })
            {
                if (inventory && playerSkinItems.Any(p => p.Value.Slot == Slot && (Item.PlayerHas(player, p.Value.UniqueId) || isVip)))
                {
                    using (new WithTemporaryCulture(player.GetLanguage()))
                    {
                        StringBuilder builder = new();
                        builder.AppendFormat(Instance.Localizer[$"menu_store<{(Slot == 2 ? "t" : "ct")}_title>"]);

                        menu.AddMenuOption(builder.ToString(), (CCSPlayerController player, ChatMenuOption option) =>
                        {
                            DisplayItem(player, inventory, isVip, builder.ToString(), playerSkinItems.Where(p => p.Value.Slot == Slot).ToDictionary(p => p.Key, p => p.Value));
                        });
                    }
                }
                else if (!inventory)
                {
                    using (new WithTemporaryCulture(player.GetLanguage()))
                    {
                        StringBuilder builder = new();
                        builder.AppendFormat(Instance.Localizer[$"menu_store<{(Slot == 2 ? "t" : "ct")}_title>"]);

                        menu.AddMenuOption(builder.ToString(), (CCSPlayerController player, ChatMenuOption option) =>
                        {
                            DisplayItem(player, inventory, isVip, builder.ToString(), playerSkinItems.Where(p => p.Value.Slot == Slot).ToDictionary(p => p.Key, p => p.Value));
                        });
                    }
                }
            }

            MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
        }
        else
        {
            DisplayItem(player, inventory, isVip, key, items);
        }
    }

    public static void DisplayItem(CCSPlayerController player, bool inventory, bool isVip, string key, Dictionary<string, Store_Item> items)
    {
        CenterHtmlMenu menu = new(key);

        foreach (KeyValuePair<string, Store_Item> kvp in items)
        {
            Store_Item item = kvp.Value;

            if (!item.Enable)
            {
                continue;
            }

            if (Item.PlayerHas(player, item.UniqueId) || isVip && item.Slot != 0)
            {
                menu.AddMenuOption(item.Name, (CCSPlayerController player, ChatMenuOption option) =>
                {
                    DisplayItemOption(player, item, isVip);
                });
            }
            else if (!inventory)
            {
                using (new WithTemporaryCulture(player.GetLanguage()))
                {
                    StringBuilder builder = new();
                    builder.AppendFormat(Instance.Localizer["menu_store<purchase>", item.Name, item.Price]);

                    menu.AddMenuOption(builder.ToString(), (CCSPlayerController player, ChatMenuOption option) =>
                    {
                        Item.Purchase(player, item);
                    });
                }
            }
        }

        MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
    }

    public static void DisplayItemOption(CCSPlayerController player, Store_Item item, bool isVip)
    {
        CenterHtmlMenu menu = new(item.Name);

        if (Item.PlayerUsing(player, item.UniqueId))
        {
            using (new WithTemporaryCulture(player.GetLanguage()))
            {
                StringBuilder builder = new();
                builder.AppendFormat(Instance.Localizer["menu_store<unequip>", item.Name]);

                menu.AddMenuOption(builder.ToString(), (CCSPlayerController player, ChatMenuOption option) =>
                {
                    Item.Unequip(player, item);

                    player.PrintToChatMessage("Purchase UnEquip", item.Name);

                    MenuManager.CloseActiveMenu(player);
                });
            }
        }
        else
        {
            using (new WithTemporaryCulture(player.GetLanguage()))
            {
                StringBuilder builder = new();
                builder.AppendFormat(Instance.Localizer["menu_store<equip>", item.Name]);

                menu.AddMenuOption(builder.ToString(), (CCSPlayerController player, ChatMenuOption option) =>
                {
                    Item.Equip(player, item);

                    player.PrintToChatMessage("Purchase Equip", item.Name);

                    MenuManager.CloseActiveMenu(player);
                });
            }
        }

        if (Instance.Config.Menu["enable_selling"] == "1" && !isVip)
        {
            using (new WithTemporaryCulture(player.GetLanguage()))
            {
                StringBuilder builder = new();
                builder.AppendFormat(Instance.Localizer["menu_store<sell>", item.Name]);

                menu.AddMenuOption(builder.ToString(), (CCSPlayerController player, ChatMenuOption option) =>
                {
                    Item.Sell(player, item);

                    player.PrintToChatMessage("Item Sell", item.Name);

                    MenuManager.CloseActiveMenu(player);
                });
            }
        }

        MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
    }
}