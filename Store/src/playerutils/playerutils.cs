using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Timers;
using System.Drawing;
using static Store.Store;

namespace Store;

public static class PlayerUtils
{
    static public void PrintToChatMessage(this CCSPlayerController player, string message, params object[] args)
    {
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            stringBuilder builder = new(Instance.Config.Tag);
            builder.AppendFormat(Instance.Localizer[message], args);
            player.PrintToChat(builder.Tostring());
        }
    }

    static public void ChangeModelDelay(this CCSPlayerController player, string model, bool disableleg, int slotNumber)
    {
        float apply_delay = float.Max(Instance.Config.Settings.ApplyPlayerskinDelay, 0.1f);

        Instance.AddTimer(apply_delay, () =>
        {
            if (player.IsValid && player.TeamNum == slotNumber && player.PawnIsAlive)
            {
                player.PlayerPawn.Value?.ChangeModel(model, disableleg);
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    static public void ChangeModel(this CCSPlayerPawn pawn, string model, bool disableleg)
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