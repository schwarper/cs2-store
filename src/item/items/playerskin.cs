using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;

namespace Store;

public partial class Store : BasePlugin
{
    private void Playerskin_OnPluginStart()
    {
        new StoreAPI().RegisterType("playerskin", Playerskin_OnMapStart, Playerskin_OnEquip, Playerskin_OnUnequip, true, true);

        RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
        {
            CCSPlayerController player = @event.Userid;

            if (player == null || !player.IsValid)
            {
                return HookResult.Continue;
            }

            CCSPlayerPawn? playerPawn = player.PlayerPawn?.Value;

            if (playerPawn == null)
            {
                return HookResult.Continue;
            }

            Store_PlayerItem? item = GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "playerskin" && p.Slot == player.TeamNum && !p.UniqueId.StartsWith("colorfulmodel"));

            if (item == null)
            {
                string[] modelsArray = player.Team == CsTeam.CounterTerrorist ? Config.DefaultModels["ct"] : Config.DefaultModels["t"];
                int maxIndex = modelsArray.Length;

                if (maxIndex > 0)
                {
                    int randomnumber = random.Next(0, maxIndex - 1);

                    Server.NextFrame(() =>
                    {
                        playerPawn.SetModel(modelsArray[randomnumber]);
                    });
                }
            }
            else
            {
                playerPawn.SetModel(item.UniqueId);
            }

            return HookResult.Continue;
        });
    }
    private void Playerskin_OnMapStart()
    {
        IEnumerable<string> playerSkinItems = Config.Items
        .SelectMany(wk => wk.Value)
        .Where(kvp => kvp.Value.Type == "playerskin")
        .Select(kvp => kvp.Value.UniqueId);

        RegisterListener<OnServerPrecacheResources>((manifest) =>
        {
            foreach (string UniqueId in playerSkinItems)
            {
                manifest.AddResource(UniqueId);
            }
        });
    }
    private bool Playerskin_OnEquip(CCSPlayerController player, Store_Item item)
    {
        if (player.PawnIsAlive && player.TeamNum == item.Slot)
        {
            Server.NextFrame(() =>
            {
                player.PlayerPawn.Value?.SetModel(item.UniqueId);
            });
        }

        return true;
    }
    private bool Playerskin_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        if (player.PawnIsAlive && player.TeamNum == item.Slot)
        {
            string[] modelsArray = player.Team == CsTeam.CounterTerrorist ? Config.DefaultModels["ct"] : Config.DefaultModels["t"];
            int maxIndex = modelsArray.Length;

            if (maxIndex > 0)
            {
                int randomnumber = random.Next(0, maxIndex - 1);

                Server.NextFrame(() =>
                {
                    player.PlayerPawn.Value?.SetModel(modelsArray[randomnumber]);
                });
            }
        }

        return true;
    }

    /*
    private void PlayerSkins_ChangeColor(CCSPlayerController player)
    {
        //GlobalPlayerSkinGlowTickrate
        GlobalPlayerSkinGlowTickrate[player.Slot]++;

        if (GlobalPlayerSkinGlowTickrate[player.Slot] % 5 != 0)
        {
            return;
        }

        var item = GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "playerskin" && p.Slot == player.TeamNum && p.UniqueId.StartsWith("colorfulmodel"));

        if (item == null)
        {
            return;
        }

        Random random = new();
        KnownColor? randomColorName = (KnownColor?)Enum.GetValues(typeof(KnownColor)).GetValue(random.Next(Enum.GetValues(typeof(KnownColor)).Length));

        if (randomColorName.HasValue)
        {
            Color color = Color.FromKnownColor(randomColorName.Value);

            CCSPlayerPawn? pawn = player.PlayerPawn.Value;

            if (pawn != null)
            {
                pawn.RenderMode = RenderMode_t.kRenderTransColor;
                pawn.Render = color;
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            }
        }
    }

    
        RegisterListener<OnTick>(() =>
        {
            GlobalTick++;

            if (GlobalTick % 5 != 0)
            {
                return;
            }

            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (!player.Valid || !player.PawnIsAlive)
                {
                    continue;
                }

                CreateTrail(player);
                PlayerSkins_ChangeColor(player);
            }
        });
    */
}