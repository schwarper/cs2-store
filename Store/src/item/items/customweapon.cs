using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Runtime.InteropServices;
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
        return Weapon.HandleEquip(player, item, true);
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return Weapon.HandleEquip(player, item, false);
    }

    public static void OnEntityCreated(CEntityInstance entity)
    {
        if (!customweaponExists || !entity.DesignerName.StartsWith("weapon_"))
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

            List<Store_Equipment> playerequipments = Item.GetPlayerEquipments(player).Where(p => p.SteamID == player.SteamID && p.Type == "customweapon").ToList();

            if (playerequipments.Count == 0)
            {
                return;
            }

            string designerName = weapon.DesignerName.Contains("bayonet") ? "weapon_knife" : weapon.DesignerName;

            CBasePlayerWeapon? activeweapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

            foreach (Store_Equipment? playerequipment in playerequipments)
            {
                Dictionary<string, string>? itemdata = Item.GetItem(playerequipment.Type, playerequipment.UniqueId);

                if (itemdata == null || !designerName.Contains(itemdata["weapon"]))
                {
                    continue;
                }

                Weapon.UpdateModel(player, weapon, itemdata["uniqueid"], weapon == activeweapon);
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
        public static bool HandleEquip(CCSPlayerController player, Dictionary<string, string> item, bool isEquip)
        {
            if (player.PawnIsAlive)
            {
                CBasePlayerWeapon? weapon = Get(player, item["weapon"]);

                if (weapon != null)
                {
                    var equip = weapon == player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

                    if (isEquip)
                    {
                        UpdateModel(player, weapon, item["uniqueid"], equip);
                    }
                    else
                    {
                        ResetWeapon(player, weapon, equip);
                    }
                }
            }

            return true;
        }
        private static CBasePlayerWeapon? Get(CCSPlayerController player, string weaponName)
        {
            var activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

            if (activeWeapon != null && activeWeapon.DesignerName.Contains(weaponName))
            {
                return activeWeapon;
            }

            return player.PlayerPawn.Value?.WeaponServices?.MyWeapons?.FirstOrDefault(p => p.Value != null && p.Value.DesignerName.Contains(weaponName))?.Value;
        }
        private static unsafe CBaseViewModel? ViewModel(CCSPlayerController player)
        {
            nint? handle = player.PlayerPawn.Value?.ViewModelServices?.Handle;

            if (handle == null || !handle.HasValue)
            {
                return null;
            }

            CCSPlayer_ViewModelServices viewModelServices = new(handle.Value);

            nint ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
            Span<nint> viewModels = MemoryMarshal.CreateSpan(ref ptr, 3);

            CHandle<CBaseViewModel> viewModel = new(viewModels[0]);

            return viewModel.Value;
        }
    }
}
