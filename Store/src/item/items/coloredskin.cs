using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using System.Drawing;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

[StoreItemType("coloredskin")]
public class Item_ColoredSkin : IItemModule
{
    public bool Equipable => true;
    public bool? RequiresAlive => null;

    private static bool _coloredSkinExists = false;

    public void OnPluginStart()
    {
        _coloredSkinExists = Item.IsAnyItemExistInType("coloredskin");
    }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest) { }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        player.PlayerPawn.Value?.ColorSkin(Color.White);
        return true;
    }

    public static void OnTick(CCSPlayerController player)
    {
        if (!_coloredSkinExists) return;

        Store_Equipment? playerColoredSkin = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "coloredskin");
        if (playerColoredSkin == null) return;

        Dictionary<string, string>? itemData = Item.GetItem(playerColoredSkin.UniqueId);
        if (itemData == null) return;

        Color color;

        if (itemData.TryGetValue("color", out string? scolor) && !string.IsNullOrEmpty(scolor))
        {
            string[] colorValues = scolor.Split(' ');
            color = Color.FromArgb(int.Parse(colorValues[0]), int.Parse(colorValues[1]), int.Parse(colorValues[2]));
        }
        else
        {
            Array knownColors = Enum.GetValues(typeof(KnownColor));
            KnownColor? randomColorName = (KnownColor?)knownColors.GetValue(Instance.Random.Next(knownColors.Length));

            if (!randomColorName.HasValue) return;

            color = Color.FromKnownColor(randomColorName.Value);
        }

        player.PlayerPawn.Value?.ColorSkin(color);
    }
}