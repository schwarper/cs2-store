using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Timers;
using System.Drawing;
using System.Text;
using static Store.Config_Config;
using static Store.Store;

namespace Store;

public static class PlayerUtils
{
    static public void PrintToChatMessage(this CCSPlayerController player, string message, params object[] args)
    {
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            StringBuilder builder = new(Config.Tag);
            builder.AppendFormat(Instance.Localizer[message], args);
            player.PrintToChat(builder.ToString());
        }
    }

    static public void ChangeModelDelay(this CCSPlayerController player, string model, bool disableleg, int slotNumber, string? skin)
    {
        float apply_delay = float.Max(Config.Settings.ApplyPlayerskinDelay, 0.1f);

        Instance.AddTimer(apply_delay, () =>
        {
            if (!player.IsValid || !player.PawnIsAlive || (slotNumber != 1 && player.TeamNum != slotNumber))
            {
                return;
            }

            player.PlayerPawn.Value?.ChangeModel(model, disableleg, skin);
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    static public void ChangeModel(this CCSPlayerPawn pawn, string model, bool disableleg, string? skin)
    {
        if (model == string.Empty)
        {
            return;
        }

        Server.NextFrame(() =>
        {
            pawn.SetModel(model);

            Color originalRender = pawn.Render;

            if (disableleg)
            {
                pawn.Render = Color.FromArgb(254, originalRender.R, originalRender.G, originalRender.B);
            }
            else
            {
                pawn.Render = Color.FromArgb(255, originalRender.R, originalRender.G, originalRender.B);
            }

            if (skin != null)
            {
                pawn.AcceptInput("Skin", null, pawn, skin);
            }
        });
    }

    static public void ColorSkin(this CCSPlayerPawn pawn, Color color)
    {
        Color originalRender = pawn.Render;

        pawn.Render = Color.FromArgb(originalRender.A, color.R, color.G, color.B);
        pawn.RenderMode = RenderMode_t.kRenderTransColor;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }
    static public int GetHealth(this CCSPlayerPawn pawn)
    {
        return pawn.Health;
    }
    static public void SetHealth(this CCSPlayerController player, int health)
    {
        if (player.PlayerPawn == null || player.PlayerPawn.Value == null)
        {
            return;
        }

        player.Health = health;
        player.PlayerPawn.Value.Health = health;

        if (health > 100)
        {
            player.MaxHealth = health;
            player.PlayerPawn.Value.MaxHealth = health;
        }

        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
    }
}