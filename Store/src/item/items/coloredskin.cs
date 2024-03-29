using CounterStrikeSharp.API.Core;
using System.Drawing;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public static void ColoredSkin_OnPluginStart()
    {
        new StoreAPI().RegisterType("coloredskin", ColoredSkin_OnMapStart, ColoredSkin_OnEquip, ColoredSkin_OnUnequip, true, null);
    }
    public static void ColoredSkin_OnMapStart()
    {
    }
    public static bool ColoredSkin_OnEquip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
    public static bool ColoredSkin_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        player.PlayerPawn.Value?.Color(Color.White);
        return true;
    }

    public static void OnTick_ColoredSkin(CCSPlayerController player)
    {
        Store_PlayerItem? playercoloredskin = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "coloredskin" && (p.Slot == 0 || (p.Slot != 0 && p.Slot == player.TeamNum)));

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