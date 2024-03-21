using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Core.Listeners;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public void Playerskin_OnPluginStart()
    {
        new StoreAPI().RegisterType("playerskin", Playerskin_OnMapStart, Playerskin_OnEquip, Playerskin_OnUnequip, true, null);

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

            Store_PlayerItem? item = GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "playerskin" && p.Slot == player.TeamNum);

            if (item == null)
            {
                SetDefaultModel(player);
            }
            else
            {
                playerPawn.ChangeModel(item.UniqueId);
            }

            return HookResult.Continue;
        });
    }
    public void Playerskin_OnMapStart()
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

    public bool Playerskin_OnEquip(CCSPlayerController player, Store_Item item)
    {
        if (player.PawnIsAlive && player.TeamNum == item.Slot)
        {
            player.PlayerPawn.Value?.ChangeModel(item.UniqueId);
        }

        return true;
    }
    public bool Playerskin_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        if (player.PawnIsAlive && player.TeamNum == item.Slot)
        {
            SetDefaultModel(player);
        }

        return true;
    }

    public void SetDefaultModel(CCSPlayerController player)
    {
        string[] modelsArray = player.Team == CsTeam.CounterTerrorist ? Config.DefaultModels["ct"] : Config.DefaultModels["t"];
        int maxIndex = modelsArray.Length;

        if (maxIndex > 0)
        {
            int randomnumber = random.Next(0, maxIndex - 1);

            string model = modelsArray[randomnumber];

            if (model != null)
            {
                player.PlayerPawn.Value?.ChangeModel(model);
            }
        }
    }
}