using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_ColoredSkin
{
    private static bool coloredskinExists = false;
    public static void OnPluginStart()
    {
        Item.RegisterType("coloredskin", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.GetItemsByType("coloredskin").Count > 0)
        {
            coloredskinExists = true;
        }
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        player.PlayerPawn.Value?.ColorSkin(Color.White);
        return true;
    }

    public static void OnTick(CCSPlayerController player)
    {
        if (!coloredskinExists)
        {
            return;
        }

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
            KnownColor? randomColorName = (KnownColor?)Enum.GetValues(typeof(KnownColor)).GetValue(Instance.Random.Next(Enum.GetValues(typeof(KnownColor)).Length));

            if (!randomColorName.HasValue)
            {
                return;
            }

            color = Color.FromKnownColor(randomColorName.Value);
        }

        player.PlayerPawn.Value?.ColorSkin(color);
    }
}