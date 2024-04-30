using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Store
{
    public static class Item_PlayerSkin
    {
        public static void OnPluginStart()
        {
            Item.RegisterType("playerskin", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);
            Instance.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        }

        public static void OnMapStart() { }

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

        public static HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid;

            if (player == null || !player.IsValid || player.TeamNum < 2)
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
                Dictionary<string, string>? itemData = Item.GetItem(item.Type, item.UniqueId);

                if (itemData == null)
                {
                    return HookResult.Continue;
                }

                SetPlayerModel(player, item.UniqueId, itemData["disable_leg"], item.Slot);
            }

            return HookResult.Continue;
        }

        public static void SetDefaultModel(CCSPlayerController player)
        {
            string[] modelsArray = player.Team == CsTeam.CounterTerrorist ? Instance.Config.DefaultModels["ct"] : Instance.Config.DefaultModels["t"];
            int maxIndex = modelsArray.Length;

            if (maxIndex > 0)
            {
                int randomNumber = Instance.Random.Next(0, maxIndex - 1);
                string model = modelsArray[randomNumber];
                SetPlayerModel(player, model, Instance.Config.Settings["default_model_disable_leg"], player.TeamNum);
            }
        }

        private static void SetPlayerModel(CCSPlayerController player, string model, string disableLeg, int slotNumber)
        {
            float applyDelay = 0.1f;

            if (Instance.Config.Settings.TryGetValue("apply_delay", out string? value) && float.TryParse(value, CultureInfo.InvariantCulture, out float delay))
            {
                applyDelay = float.Max(0.1f, delay);
            }

            Instance.AddTimer(applyDelay, () =>
            {
                if (player.IsValid && player.TeamNum == slotNumber && player.PawnIsAlive)
                {
                    player.PlayerPawn.Value?.ChangeModel(model, disableLeg);
                }
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }
    }
}
