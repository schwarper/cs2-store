using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_Bunnyhop
{
    private static bool bunnyhopExists = false;

    public static void OnPluginStart()
    {
        Item.RegisterType("bunnyhop", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, false);

        if (Item.GetItemsByType("bunnyhop").Count > 0)
        {
            bunnyhopExists = true;
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
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
    public static void OnTick(CCSPlayerController player)
    {
        if (!bunnyhopExists)
        {
            return;
        }

        Store_Equipment? playerbunnyhop = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "bunnyhop");

        if (playerbunnyhop == null)
        {
            return;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
        {
            return;
        }

        PlayerFlags flags = (PlayerFlags)playerPawn.Flags;
        PlayerButtons buttons = player.Buttons;

        if (!buttons.HasFlag(PlayerButtons.Jump) || !flags.HasFlag(PlayerFlags.FL_ONGROUND) || playerPawn.MoveType.HasFlag(MoveType_t.MOVETYPE_LADDER))
        {
            return;
        }

        playerPawn.AbsVelocity.Z = 267.0f;
    }
}