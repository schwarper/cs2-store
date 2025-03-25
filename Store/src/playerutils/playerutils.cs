using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using static Store.Config_Config;
using static Store.Store;

namespace Store;

public static class PlayerUtils
{
    public static void PrintToChatMessage(this CCSPlayerController player, string message, params object[] args)
    {
        player.PrintToChat($"{Config.Tag}{Instance.Localizer.ForPlayer(player, message, args)}");
    }

    public static void ChangeModelDelay(this CCSPlayerController player, string model, bool disableLeg, int slotNumber, string? skin)
    {
        float applyDelay = Math.Max(Config.Settings.ApplyPlayerskinDelay, 0.1f);

        Instance.AddTimer(applyDelay, () =>
        {
            if (!player.IsValid || !player.PawnIsAlive || (slotNumber != 1 && player.TeamNum != slotNumber))
            {
                return;
            }

            player.PlayerPawn.Value?.ChangeModel(model, disableLeg, skin);
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    public static void ChangeModel(this CCSPlayerPawn pawn, string model, bool disableLeg, string? skin)
    {
        if (string.IsNullOrEmpty(model)) return;

        Server.NextFrame(() =>
        {
            pawn.SetModel(model);

            Color originalRender = pawn.Render;

            pawn.Render = disableLeg
                ? Color.FromArgb(254, originalRender.R, originalRender.G, originalRender.B)
                : Color.FromArgb(255, originalRender.R, originalRender.G, originalRender.B);

            if (!string.IsNullOrEmpty(skin))
            {
                pawn.AcceptInput("Skin", null, pawn, skin);
            }
        });
    }

    public static void ColorSkin(this CCSPlayerPawn pawn, Color color)
    {
        Color originalRender = pawn.Render;

        pawn.Render = Color.FromArgb(originalRender.A, color.R, color.G, color.B);
        pawn.RenderMode = RenderMode_t.kRenderTransColor;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    public static int GetHealth(this CCSPlayerPawn pawn)
    {
        return pawn.Health;
    }

    public static void SetHealth(this CCSPlayerController player, int health)
    {
        if (player.PlayerPawn?.Value is not { } pawn) return;

        player.Health = health;
        pawn.Health = health;

        if (health > 100)
        {
            player.MaxHealth = health;
            pawn.MaxHealth = health;
        }

        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }

    public static void GiveArmor(this CCSPlayerPawn playerPawn, int armor)
    {
        if (playerPawn.ItemServices != null)
        {
            new CCSPlayer_ItemServices(playerPawn.ItemServices.Handle).HasHelmet = true;
        }

        playerPawn.ArmorValue += armor;
        Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_ArmorValue");
    }

    public static void BunnyHop(this CCSPlayerPawn playerPawn, CCSPlayerController player)
    {
        PlayerFlags flags = (PlayerFlags)playerPawn.Flags;
        PlayerButtons buttons = player.Buttons;

        if (!buttons.HasFlag(PlayerButtons.Jump) || !flags.HasFlag(PlayerFlags.FL_ONGROUND) || playerPawn.MoveType.HasFlag(MoveType_t.MOVETYPE_LADDER))
        {
            return;
        }

        playerPawn.AbsVelocity.Z = 267.0f;
    }
}