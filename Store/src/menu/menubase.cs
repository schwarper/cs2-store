using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Enum;
using CS2MenuManager.API.Interface;
using CS2MenuManager.API.Menu;
using System.Text.Json;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class MenuBase
{
    public static List<JsonProperty> GetElementJsonProperty(JsonElement element)
    {
        return [.. element.EnumerateObject().Where(prop => prop.Name != "flag" && prop.Name != "langname")];
    }

    public static void DisplayStoreMenu(CCSPlayerController? player, bool inventory)
    {
        if (player == null)
            return;

        if (!MenuTypes.TryGetValue(Config.Menu.MenuType, out Type? menuType) || menuType == null)
        {
            throw new InvalidOperationException(
                "Invalid menu type configured. Please use one of the following valid menu types:\n" +
                string.Join(" ,", MenuTypes.Keys) + "\n" +
                $"Configured menu type: '{Config.Menu.MenuType}'"
            );
        }

        Menu.DisplayStore(player, inventory, menuType);
    }

    public static int GetSellingPrice(Dictionary<string, string> item, Store_Item playerItem)
    {
        float sellRatio = Config.Settings.SellRatio;
        bool usePurchaseCredit = Config.Settings.SellUsePurchaseCredit;

        int purchasePrice = usePurchaseCredit && playerItem != null ? playerItem.Price : int.Parse(item["price"]);
        return (int)(purchasePrice * sellRatio);
    }

    public static bool CheckFlag(CCSPlayerController player, Dictionary<string, string> item, bool sell = false)
    {
        item.TryGetValue("flag", out string? flag);
        return CheckFlag(player, flag, !sell);
    }

    public static bool CheckFlag(CCSPlayerController player, string? flagAll, bool trueIfNull)
    {
        return string.IsNullOrEmpty(flagAll)
            ? trueIfNull
            : flagAll.Split(',')
            .Any(flag => (flag.StartsWith('@') && AdminManager.PlayerHasPermissions(player, flag)) ||
                         (flag.StartsWith('#') && AdminManager.PlayerInGroup(player, flag)) ||
                         (flag == player.SteamID.ToString()));
    }

    public static string GetCategoryName(CCSPlayerController player, JsonProperty category)
    {
        string name = category.Name;

        return name.StartsWith('*') && name.EndsWith('*') ? Instance.Localizer.ForPlayer(player, name) : name;
    }

    public static void InspectAction(CCSPlayerController player, Dictionary<string, string> item, string type)
    {
        switch (type)
        {
            case "playerskin":
                item.TryGetValue("skin", out string? skn);
                Item_PlayerSkin.Inspect(player, item["model"], skn);
                break;
            case "customweapon":
                Item_CustomWeapon.Inspect(player, item["viewmodel"], item["weapon"]);
                break;
        }
    }

    public static IMenu CreateMenuByType(Type menuType, string title, bool displayResolutionMenu = false)
    {
        if (menuType == typeof(ChatMenu))
            return new ChatMenu(title, Instance);
        if (menuType == typeof(ConsoleMenu))
            return new ConsoleMenu(title, Instance);
        return menuType == typeof(CenterHtmlMenu)
            ? new CenterHtmlMenu(title, Instance)
            : menuType == typeof(WasdMenu)
            ? new WasdMenu(title, Instance)
            : menuType == typeof(ScreenMenu) ? new ScreenMenu(title, Instance) { ShowResolutionsOption = displayResolutionMenu } : (IMenu)null!;
    }

    public static void AddMenuOption(this IMenu menu, CCSPlayerController player, DisableOption disableOption, string display, params object[] args)
    {
        menu.AddItem(Instance.Localizer.ForPlayer(player, display, args), disableOption);
    }

    public static void AddMenuOption(this IMenu menu, CCSPlayerController player, Action<CCSPlayerController, ItemOption> callback, string display, params object[] args)
    {
        menu.AddItem(Instance.Localizer.ForPlayer(player, display, args), callback);
    }
}
