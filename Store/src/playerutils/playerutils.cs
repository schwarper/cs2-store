using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using System.Drawing;
using System.Text;
using static Store.Store;

namespace Store;

public static class PlayerUtils
{
    static public bool Valid(this CCSPlayerController player)
    {
        return player.IsValid && player.SteamID.ToString().Length == 17;
    }
    static public bool IsPistolRound()
    {
        var gamerules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
            .First().GameRules;
        var halftime = ConVar.Find("mp_halftime")!.GetPrimitiveValue<bool>();
        var maxrounds = ConVar.Find("mp_maxrounds")!.GetPrimitiveValue<int>();

        if (gamerules == null) return false;
        return gamerules.TotalRoundsPlayed == 0 ||
               (halftime && maxrounds / 2 == gamerules.TotalRoundsPlayed) ||
               gamerules.GameRestart;
    }
    static public void PrintToChatMessage(this CCSPlayerController player, string message, params object[] args)
    {
        using (new WithTemporaryCulture(player.GetLanguage()))
        {
            StringBuilder builder = new(Instance.Localizer["Prefix"]);
            builder.AppendFormat(Instance.Localizer[message], args);
            player.PrintToChat(builder.ToString());
        }
    }
    static public void ChangeModel(this CCSPlayerPawn pawn, string model)
    {
        Server.NextFrame(() =>
        {
            if (pawn.OriginalController.Value?.PawnIsAlive == false)
            {
                return;
            }

            pawn.SetModel(model);
        });
    }

    static public void Color(this CCSPlayerPawn pawn, Color color)
    {
        pawn.RenderMode = RenderMode_t.kRenderTransColor;
        pawn.Render = color;
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