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

        List<JsonProperty> items = elementData.EnumerateObject()
            .Where(prop => prop.Name != "flag" && prop.Name != "langname")
            .ToList();

        foreach (JsonProperty item in items)
        {
            if (item.Value.TryGetProperty("flag", out JsonElement flagElement) && !Menu.CheckFlag(player, flagElement.ToString(), true))
                continue;

            if (item.Value.TryGetProperty("uniqueid", out JsonElement uniqueIdElement))
            {
                menu.AddItems(player, uniqueIdElement, inventory);
                continue;
            }

            if (inventory && !Item.PlayerHasAny(player, item.Value))
                continue;

            string categoryName = Menu.GetCategoryName(player, item);
            menu.AddMenuOption(categoryName, (p, o) => OpenMenu(p, categoryName, item.Value, inventory));
        }

        menu.Open(player);
    }

    public static void AddItems(this CenterHtmlMenu menu, CCSPlayerController player, JsonElement uniqueIdElement, bool inventory)
    {
        Dictionary<string, string> item = Instance.Items[uniqueIdElement.ToString()];

        if (item["enable"] != "true" || !Menu.CheckFlag(player, item) || (inventory && !Item.PlayerHas(player, item["type"], item["uniqueid"], false)))
            return;

        if (Item.PlayerHas(player, item["type"], item["uniqueid"], false))
        {
            menu.AddMenuOption(player, (p, o) =>
            {
                p.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(p, item);
            }, false, Item.GetItemName(player, item));
        }
        else if (!inventory && !item.IsHidden())
        {
            menu.AddMenuOption(player, (p, o) => SelectPurchase(p, item, int.Parse(item["price"]) > 0), false,
                int.Parse(item["price"]) <= 0 ? "menu_store<purchase1>" : "menu_store<purchase>",
                Item.GetItemName(player, item), item["price"]);
        }
    }

    private static void SelectPurchase(CCSPlayerController player, Dictionary<string, string> item, bool confirm)
    {
        player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");

        if (confirm && Config.Menu.EnableConfirmMenu)
            DisplayConfirmationMenu(player, item);
        else if (Item.Purchase(player, item))
            DisplayItemOption(player, item);
        else if (item["price"] == "0")
        {
            Store.Api.PlayerPurchaseItem(player, item);
            player.PrintToChatMessage("Purchase Succeeded", Item.GetItemName(player, item));
        }
        else
            player.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
    }

    public static void DisplayItemOption(CCSPlayerController player, Dictionary<string, string> item)
    {
        CenterHtmlMenu menu = new(Item.GetItemName(player, item), Instance);

        menu.AddInspectOption(player, item);

        if (Item.PlayerUsing(player, item["type"], item["uniqueid"]))
        {
            menu.AddMenuOption(player, (p, o) =>
            {
                p.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                Item.Unequip(p, item, true);
                p.PrintToChatMessage("Purchase Unequip", Item.GetItemName(p, item));
                DisplayItemOption(p, item);
            }, false, "menu_store<unequip>");
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
                DisplayItemOption(p, item);
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
                    menu.AddMenuOption(player, (p, o) =>
                    {
                        p.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                        Item.Sell(p, item);
                        p.PrintToChatMessage("Item Sell", Item.GetItemName(p, item));
                        MenuManager.CloseActiveMenu(p);
                    }, false, "menu_store<sell>", sellingPrice);
                }
            }

            if (playerItem.DateOfExpiration > DateTime.MinValue)
                menu.AddMenuOption(playerItem.DateOfExpiration.ToString(), (p, o) => { }, true);
        }

        menu.Open(player);
    }

    public static void DisplayConfirmationMenu(CCSPlayerController player, Dictionary<string, string> item)
    {
        CenterHtmlMenu menu = new(Instance.Localizer.ForPlayer(player, "menu_store<confirm_title>"), Instance);

        menu.AddMenuOption(player, (p, o) => { }, true, "menu_store<confirm_item>", Item.GetItemName(player, item), item["price"]);
        menu.AddInspectOption(player, item);

        menu.AddMenuOption(player, (p, o) =>
        {
            if (Item.Purchase(p, item))
            {
                p.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundYes}");
                DisplayItemOption(p, item);
            }
            else
            {
                MenuManager.CloseActiveMenu(p);
                p.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            }
        }, false, "menu_store<yes>");

        menu.AddMenuOption(player, (p, o) =>
        {
            p.ExecuteClientCommand($"play {Config.Menu.MenuPressSoundNo}");
            MenuManager.CloseActiveMenu(p);
        }, false, "menu_store<no>");

        menu.Open(player);
    }

    public static void AddInspectOption(this CenterHtmlMenu menu, CCSPlayerController player, Dictionary<string, string> item)
    {
        if (item["type"] == "playerskin" || item["type"] == "customweapon")
        {
            float waitTime = 0.0f;

            menu.AddMenuOption(player, (p, o) =>
            {
                if (Server.CurrentTime < waitTime)
                    return;

                waitTime = Server.CurrentTime + 5.0f;
                Menu.InspectAction(p, item, item["type"]);
            }, false, "menu_store<inspect>");
        }
    }
}