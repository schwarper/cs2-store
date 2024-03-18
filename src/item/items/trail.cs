using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using static CounterStrikeSharp.API.Core.Listeners;

namespace Store;

public partial class Store : BasePlugin
{
    private static int TrailTickrate = 0;
    readonly Vector[] TrailVector = new Vector[64];

    private void Trail_OnPluginStart()
    {
        for (int i = 0; i < 64; i++)
        {
            TrailVector[i] = new();
        }

        new StoreAPI().RegisterType("trail", Trail_OnMapStart, Trail_OnEquip, Trail_OnUnequip, false, true);

        RegisterListener<OnTick>(() =>
        {
            TrailTickrate++;

            if (TrailTickrate % 5 != 0)
            {
                return;
            }

            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (!player.Valid() || !player.PawnIsAlive)
                {
                    continue;
                }

                CreateTrail(player);
            }
        });
    }
    private void Trail_OnMapStart()
    {
    }
    private bool Trail_OnEquip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
    private bool Trail_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
    private void CreateTrail(CCSPlayerController player)
    {
        Store_PlayerItem? playertrail = GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "trail");

        if (playertrail == null)
        {
            return;
        }

        CBeam? beam = Utilities.CreateEntityByName<CBeam>("env_beam");

        if (beam == null)
        {
            return;
        }

        CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null)
        {
            return;
        }

        if (playerPawn.AbsOrigin == null)
        {
            return;
        }

        if (TrailVector[player.Slot].X == 0.0 && TrailVector[player.Slot].Y == 0.0 && TrailVector[player.Slot].Z == 0.0)
        {
            TrailVector[player.Slot].X = playerPawn.AbsOrigin.X;
            TrailVector[player.Slot].Y = playerPawn.AbsOrigin.Y;
            TrailVector[player.Slot].Z = playerPawn.AbsOrigin.Z;

            return;
        }

        Color color;

        if (playertrail.UniqueId == "colortrail")
        {
            Random random = new();
            KnownColor? randomColorName = (KnownColor?)Enum.GetValues(typeof(KnownColor)).GetValue(random.Next(Enum.GetValues(typeof(KnownColor)).Length));

            if (!randomColorName.HasValue)
            {
                return;
            }

            color = Color.FromKnownColor(randomColorName.Value);
        }
        else
        {
            string[] colorString = playertrail.Color.Split(' ');

            color = Color.FromArgb(int.Parse(colorString[0]), int.Parse(colorString[1]), int.Parse(colorString[2]));
        }

        beam.RenderMode = RenderMode_t.kRenderTransColor;
        beam.Width = 1.0f;
        beam.Render = color;

        beam.Teleport(playerPawn.AbsOrigin, new QAngle(), new Vector());

        beam.EndPos.X = TrailVector[player.Slot].X;
        beam.EndPos.Y = TrailVector[player.Slot].Y;
        beam.EndPos.Z = TrailVector[player.Slot].Z;

        Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");

        TrailVector[player.Slot].X = playerPawn.AbsOrigin.X;
        TrailVector[player.Slot].Y = playerPawn.AbsOrigin.Y;
        TrailVector[player.Slot].Z = playerPawn.AbsOrigin.Z;

        AddTimer(1.3f, () =>
        {
            if (beam != null && beam.DesignerName == "env_beam")
            {
                beam.Remove();
            }
        });
    }
}