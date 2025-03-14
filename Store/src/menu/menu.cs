using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using System.Text.Json;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Menu
{
    public static List<JsonProperty> GetElementJsonProperty(JsonElement element) =>
        [.. element.EnumerateObject().Where(prop => prop.Name != "flag" && prop.Name != "langname")];

    public static void DisplayStoreMenu(CCSPlayerController? player, bool inventory)
    {
        if (player == null)
            return;

        string menutype = Config.Menu.MenuType.ToLower();
        
        try 
        {
            CS2ScreenMenuAPI.MenuAPI.CloseActiveMenu(player);
        } 
        catch {}

        switch (menutype)
        {
            case "html":
            case "center":
                CenterMenu.DisplayStore(player, inventory);
                break;

            case "worldtext":
            case "screen":
            case "screenmenu":
                ScreenTextMenu.DisplayStore(player, inventory);
                break;

            default:
                throw new InvalidOperationException(
                    "Invalid menu type configured. Please use one of the following valid menu types:\n" +
                    "- 'html' or 'center': Center HTML menu\n" +
                    "- 'worldtext', 'screen', or 'screenmenu': Screen text menu\n" +
                    $"Configured menu type: '{menutype}'"
                );
        }
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
}
