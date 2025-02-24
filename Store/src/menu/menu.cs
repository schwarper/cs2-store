using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using static Store.Config_Config;
using static StoreApi.Store;

namespace Store;

public static class Menu
{
    public static void DisplayStoreMenu(CCSPlayerController? player, bool inventory)
    {
        if (player == null)
        {
            return;
        }

        string menutype = Config.Menu.MenuType.ToLower();

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
        float sell_ratio = Config.Settings.SellRatio;

        int purchase_price = 1;

        bool usePurchaseCredit = Config.Settings.SellUsePurchaseCredit;

        if (usePurchaseCredit && playerItem != null)
        {
            purchase_price = playerItem.Price;
        }

        return (int)((usePurchaseCredit ? purchase_price : int.Parse(item["price"])) * sell_ratio);
    }

    public static bool CheckFlag(CCSPlayerController player, Dictionary<string, string> item, bool sell = false)
    {
        item.TryGetValue("flag", out string? flag);

        return CheckFlag(player, flag, !sell);
    }

    public static bool CheckFlag(CCSPlayerController player, string? flagAll, bool trueifNull)
    {
        if (string.IsNullOrEmpty(flagAll))
        {
            return trueifNull;
        }

        string[] flags = flagAll.Split(',');

        foreach (string flag in flags)
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