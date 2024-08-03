using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using static Store.Store;

namespace Store;

public static class Item_Bunnyhop
{
    private static bool bunnyhopExists = false;
    private static readonly List<CCSPlayerController> playersList = [];

    public static void OnPluginStart()
    {
        Item.RegisterType("bunnyhop", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, false, true);

        if (Item.GetItemsByType("bunnyhop").Count > 0)
        {
            bunnyhopExists = true;

            Instance.RegisterEventHandler<EventRoundStart>(OnRoundStart);
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
        if (playersList.Contains(player))
        {
            return false;
        }

        playersList.Add(player);
        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
    private static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        playersList.Clear();
        return HookResult.Continue;
    }
    public static void OnTick(CCSPlayerController player)
    {
        if (!bunnyhopExists)
        {
            return;
        }

        if (!playersList.Contains(player))
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