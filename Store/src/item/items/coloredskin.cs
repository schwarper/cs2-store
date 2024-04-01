using CounterStrikeSharp.API.Core;
using System.Drawing;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public static void ColoredSkin_OnPluginStart()
    {
        new StoreAPI().RegisterType("coloredskin", ColoredSkin_OnMapStart, ColoredSkin_OnEquip, ColoredSkin_OnUnequip, true, null);
        new StoreAPI().RegisterType("coloredskin_color", ColoredSkin_OnMapStart, ColoredSkin_OnEquip, ColoredSkin_OnUnequip, false, null);
    }
    public static void ColoredSkin_OnMapStart()
    {
    }
    public static bool ColoredSkin_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (item.TryGetValue("color", out string? color) && !string.IsNullOrEmpty(color))
        {
            string[] colorValues = item["color"].Split(' ');
            Color cColor = Color.FromArgb(int.Parse(colorValues[0]), int.Parse(colorValues[1]), int.Parse(colorValues[2]));

            player.PlayerPawn.Value?.Color(cColor);
        }
        return true;
    }
    public static bool ColoredSkin_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        player.PlayerPawn.Value?.Color(Color.White);
        return true;
    }

    public static void OnTick_ColoredSkin(CCSPlayerController player)
    {
        Store_Equipment? playercoloredskin = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "coloredskin");

        if (playercoloredskin == null)
        {
            return;
        }

        KnownColor? randomColorName = (KnownColor?)Enum.GetValues(typeof(KnownColor)).GetValue(Instance.random.Next(Enum.GetValues(typeof(KnownColor)).Length));

        if (!randomColorName.HasValue)
        {
            return;
        }

        Color color = Color.FromKnownColor(randomColorName.Value);

        player.PlayerPawn.Value?.Color(color);
    }
}