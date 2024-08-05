using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Runtime.InteropServices;
using static CounterStrikeSharp.API.Core.Listeners;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_CustomWeapon
{
    private static bool customweaponExists = false;

    public static void OnPluginStart()
    {
        Item.RegisterType("customweapon", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.GetItemsByType("customweapon").Count > 0)
        {
            if (CoreConfig.FollowCS2ServerGuidelines)
            {
                throw new Exception($"Cannot set or get 'CEconEntity::m_OriginalOwnerXuidLow' with \"FollowCS2ServerGuidelines\" option enabled.");
            }

            Instance.RegisterEventHandler<EventItemEquip>(OnItemEquip);

            customweaponExists = true;
        }
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("customweapon");

        foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
        {
            manifest.AddResource(item.Value["uniqueid"]);
        }
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (player.PawnIsAlive)
        {
            CBasePlayerWeapon? weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

            if (weapon != null && weapon.DesignerName.Contains(item["weapon"]))
            {
                Weapon.UpdateModel(player, weapon, item["uniqueid"], true);
            }
        }

        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (player.PawnIsAlive)
        {
            CBasePlayerWeapon? weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

            if (weapon != null && weapon.DesignerName.Contains(item["weapon"]))
            {
                Weapon.ResetWeapon(player, weapon, true);
            }
        }

        return true;
    }

    public static void OnEntityCreated(CEntityInstance entity)
    {
        if (!customweaponExists)
        {
            return;
        }

        if (!entity.DesignerName.StartsWith("weapon_"))
        {
            return;
        }

        CBasePlayerWeapon weapon = entity.As<CBasePlayerWeapon>();

        Server.NextWorldUpdate(() =>
        {
            if (!weapon.IsValid || weapon.OriginalOwnerXuidLow <= 0)
            {
                return;
            }

            CCSPlayerController? player = Utilities.GetPlayerFromSteamId(weapon.OriginalOwnerXuidLow);

            if (player == null)
            {
                return;
            }

            var playerequipments = Item.GetPlayerEquipments(player).Where(p => p.SteamID == player.SteamID && p.Type == "customweapon");

            if (!playerequipments.Any())
            {
                return;
            }

            var designerName = weapon.DesignerName;

            if (designerName.Contains("bayonet"))
            {
                designerName = "weapon_knife";
            }

            CBasePlayerWeapon? activeweapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

            foreach (var playerequipment in playerequipments)
            {
                Dictionary<string, string>? itemdata = Item.GetItem(playerequipment.Type, playerequipment.UniqueId);

                if (itemdata == null)
                {
                    continue;
                }

                if (!designerName.Contains(itemdata["weapon"]))
                {
                    continue;
                }

                if (activeweapon != null && weapon == activeweapon)
                {
                    Weapon.UpdateModel(player, activeweapon, itemdata["uniqueid"], true);
                }
                else
                {
                    Weapon.UpdateModel(player, weapon, itemdata["uniqueid"], false);
                }

                break;
            }
        });
    }
    public static HookResult OnItemEquip(EventItemEquip @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null)
        {
            return HookResult.Continue;
        }

        CBasePlayerWeapon? activeweapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

        if (activeweapon == null)
        {
            return HookResult.Continue;
        }

        string globalname = activeweapon.Globalname;

        if (!string.IsNullOrEmpty(globalname))
        {
            Weapon.SetViewModel(player, globalname.Split(',')[1]);
        }

        return HookResult.Continue;
    }

    public static class Weapon
    {
        public static unsafe string GetViewModel(CCSPlayerController player)
        {
            return ViewModel(player)?.VMName ?? string.Empty;
        }
        public static unsafe void SetViewModel(CCSPlayerController player, string model)
        {
            ViewModel(player)?.SetModel(model);
        }
        public static void UpdateModel(CCSPlayerController player, CBasePlayerWeapon weapon, string model, bool update)
        {
            weapon.Globalname = $"{GetViewModel(player)},{model}";
            weapon.SetModel(model);

            if (update)
            {
                SetViewModel(player, model);
            }
        }
        public static void ResetWeapon(CCSPlayerController player, CBasePlayerWeapon weapon, bool update)
        {
            string globalname = weapon.Globalname;

            if (string.IsNullOrEmpty(globalname))
            {
                return;
            }

            string[] globalnamedata = globalname.Split(',');

            weapon.Globalname = string.Empty;
            weapon.SetModel(globalnamedata[0]);

            if (update)
            {
                SetViewModel(player, globalnamedata[0]);
            }
        }
        private static unsafe CBaseViewModel? ViewModel(CCSPlayerController player)
        {
            var handle = player.PlayerPawn.Value?.ViewModelServices?.Handle;

            if (handle == null || !handle.HasValue)
            {
                return null;
            }

            CCSPlayer_ViewModelServices viewModelServices = new CCSPlayer_ViewModelServices(handle.Value);

            nint ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
            Span<nint> viewModels = MemoryMarshal.CreateSpan(ref ptr, 3);

            var viewModel = new CHandle<CBaseViewModel>(viewModels[0]);

            return viewModel.Value;
        }
    }
}