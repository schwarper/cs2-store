using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_PlayerSkin
{
    public static void OnPluginStart()
    {
        Item.RegisterType("playerskin", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        Instance.RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
        {
            CCSPlayerController player = @event.Userid;

            if (player == null || !player.IsValid)
            {
                return HookResult.Continue;
            }

            if (player.TeamNum < 2)
            {
                return HookResult.Continue;
            }

            CCSPlayerPawn? playerPawn = player.PlayerPawn?.Value;

            if (playerPawn == null)
            {
                return HookResult.Continue;
            }

            Store_Equipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "playerskin" && p.Slot == player.TeamNum);

            if (item == null)
            {
                SetDefaultModel(player);
            }
            else
            {
                Dictionary<string, string>? itemdata = Item.GetItem(item.Type, item.UniqueId);

                if (itemdata == null)
                {
                    return HookResult.Continue;
                }

                SetPlayerModel(player, item.UniqueId, itemdata["disable_leg"], item.Slot);
            }

            return HookResult.Continue;
        });
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("playerskin");

        foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
        {
            manifest.AddResource(item.Value["uniqueid"]);

            if (item.Value.TryGetValue("armModel", out string? armModel) && !string.IsNullOrEmpty(armModel))
            {
                manifest.AddResource(armModel);
            }
        }
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!item.TryGetValue("slot", out string? slot) || string.IsNullOrEmpty(slot))
        {
            return false;
        }

        SetPlayerModel(player, item["uniqueid"], item["disable_leg"], int.Parse(item["slot"]));
        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (!item.TryGetValue("slot", out string? slot) || string.IsNullOrEmpty(slot))
        {
            return false;
        }

        if (player.TeamNum == int.Parse(item["slot"]))
        {
            SetDefaultModel(player);
        }

        return true;
    }
    public static void SetDefaultModel(CCSPlayerController player)
    {
        string[] modelsArray = player.Team == CsTeam.CounterTerrorist ? Instance.Config.DefaultModels["ct"] : Instance.Config.DefaultModels["t"];
        int maxIndex = modelsArray.Length;

        if (maxIndex > 0)
        {
            int randomnumber = Instance.Random.Next(0, maxIndex - 1);

            string model = modelsArray[randomnumber];

            SetPlayerModel(player, model, Instance.Config.Settings["default_model_disable_leg"], player.TeamNum);
        }
    }
    private static void SetPlayerModel(CCSPlayerController player, string model, string disable_leg, int slotNumber)
    {
        float apply_delay = 0.0f;

        if (Instance.Config.Settings.TryGetValue("apply_delay", out string? value) && float.TryParse(value, CultureInfo.InvariantCulture, out float delay))
        {
            apply_delay = delay;
        }

        if (apply_delay > 0.0)
        {
            Instance.AddTimer(apply_delay, () =>
            {
                if (player == null || !player.IsValid || player.PlayerPawn.Value == null || player.TeamNum < 2 || !player.PawnIsAlive)
                {
                    return;
                }
                if (player.TeamNum == slotNumber)
                {
                    player.PlayerPawn.Value.ChangeModel(model, disable_leg);
                }

            }, TimerFlags.STOP_ON_MAPCHANGE);
        }
        else
        {
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null || player.TeamNum < 2 || !player.PawnIsAlive)
            {
                return;
            }
            if (player.TeamNum == slotNumber)
            {
                player.PlayerPawn.Value.ChangeModel(model, disable_leg);
            }
        }
    }
}