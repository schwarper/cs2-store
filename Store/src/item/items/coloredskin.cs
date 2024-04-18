using CounterStrikeSharp.API.Core;
using System.Drawing;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public static void ColoredSkin_OnPluginStart()
    {
        Item.RegisterType("coloredskin", ColoredSkin_OnMapStart, ColoredSkin_OnEquip, ColoredSkin_OnUnequip, true, null);
    }
    public static void ColoredSkin_OnMapStart()
    {
    }
    public static bool ColoredSkin_OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
    public static bool ColoredSkin_OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        player.PlayerPawn.Value?.ColorSkin(Color.White);
        return true;
    }

    public static void OnTick_ColoredSkin(CCSPlayerController player)
    {
        Store_Equipment? playercoloredskin = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "coloredskin");

        if (playercoloredskin == null)
        {
            return;
        }

        Dictionary<string, string>? itemdata = Item.GetItem(playercoloredskin.Type, playercoloredskin.UniqueId);

        if (itemdata == null)
        {
            return;
        }

        Color color;

        if (itemdata.TryGetValue("color", out string? scolor) && !string.IsNullOrEmpty(scolor))
        {
            string[] colorValues = scolor.Split(' ');

            color = Color.FromArgb(int.Parse(colorValues[0]), int.Parse(colorValues[1]), int.Parse(colorValues[2]));
        }
        else
        {
            KnownColor? randomColorName = (KnownColor?)Enum.GetValues(typeof(KnownColor)).GetValue(Instance.random.Next(Enum.GetValues(typeof(KnownColor)).Length));

            if (!randomColorName.HasValue)
            {
                return;
            }

            color = Color.FromKnownColor(randomColorName.Value);
        }

        player.PlayerPawn.Value?.ColorSkin(color);
    }
}