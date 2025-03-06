using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Runtime.InteropServices;
using static Store.Store;

namespace Store;

public static class Item_CustomWeapon
{
    private static bool _customWeaponExists = false;

    public static void OnPluginStart()
    {
        Item.RegisterType("customweapon", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.IsAnyItemExistInType("customweapon"))
        {
            if (CoreConfig.FollowCS2ServerGuidelines)
            {
                throw new Exception("Cannot set or get 'CEconEntity::m_OriginalOwnerXuidLow' with \"FollowCS2ServerGuidelines\" option enabled.");
            }

            Instance.RegisterEventHandler<EventItemEquip>(OnItemEquip);
            _customWeaponExists = true;
        }
    }

    public static void OnMapStart() { }

    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
        List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("customweapon");

        foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
        {
            manifest.AddResource(item.Value["viewmodel"]);

            if (item.Value.TryGetValue("worldmodel", out string? worldmodel) && !string.IsNullOrEmpty(worldmodel))
            {
                manifest.AddResource(worldmodel);
            }
        }
    }

    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item) =>
        Weapon.HandleEquip(player, item, true);

    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update) =>
        !update || Weapon.HandleEquip(player, item, false);

    public static void OnEntityCreated(CEntityInstance entity)
    {
        if (!_customWeaponExists || !entity.DesignerName.StartsWith("weapon_")) return;

        Server.NextWorldUpdate(() =>
        {
            CBasePlayerWeapon weapon = new(entity.Handle);

            if (!weapon.IsValid || weapon.OriginalOwnerXuidLow <= 0) return;

            CCSPlayerController? player = FindTarget.FindTargetFromWeapon(weapon);

            if (player == null) return;

            CBasePlayerWeapon? activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
            List<StoreApi.Store.Store_Equipment> playerEquipments = [.. Item.GetPlayerEquipments(player).Where(p => p.SteamID == player.SteamID && p.Type == "customweapon")];

            foreach (StoreApi.Store.Store_Equipment? playerEquipment in playerEquipments)
            {
                Dictionary<string, string>? itemData = Item.GetItem(playerEquipment.UniqueId);
                if (itemData == null) continue;

                string weaponDesignerName = Weapon.GetDesignerName(weapon);
                if (!weaponDesignerName.Contains(itemData["weapon"])) continue;

                itemData.TryGetValue("worldmodel", out string? worldmodel);
                Weapon.UpdateModel(player, weapon, itemData["viewmodel"], worldmodel, weapon == activeWeapon);
            }
        });
    }

    public static HookResult OnItemEquip(EventItemEquip @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null) return HookResult.Continue;

        string? globalName = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value?.Globalname;
        if (!string.IsNullOrEmpty(globalName))
        {
            string model = Weapon.GetFromGlobalName(globalName, Weapon.GlobalNameData.ViewModel);
            Weapon.SetViewModel(player, model);
        }

        return HookResult.Continue;
    }

    public static class Weapon
    {
        public enum GlobalNameData
        {
            ViewModelDefault,
            ViewModel,
            WorldModel
        }

        public static string GetDesignerName(CBasePlayerWeapon weapon)
        {
            string weaponDesignerName = weapon.DesignerName;
            ushort weaponIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;

            return (weaponDesignerName, weaponIndex) switch
            {
                var (name, _) when name.Contains("bayonet") => "weapon_knife",
                ("weapon_m4a1", 60) => "weapon_m4a1_silencer",
                ("weapon_hkp2000", 61) => "weapon_usp_silencer",
                ("weapon_mp7", 23) => "weapon_mp5sd",
                _ => weaponDesignerName
            };
        }

        public static string GetFromGlobalName(string globalName, GlobalNameData data)
        {
            string[] globalNameSplit = globalName.Split(',');

            return data switch
            {
                GlobalNameData.ViewModelDefault => globalNameSplit[0],
                GlobalNameData.ViewModel => globalNameSplit[1],
                GlobalNameData.WorldModel => !string.IsNullOrEmpty(globalNameSplit[2]) ? globalNameSplit[2] : globalNameSplit[1],
                _ => throw new NotImplementedException()
            };
        }

        public static unsafe string GetViewModel(CCSPlayerController player) =>
            ViewModel(player)?.VMName ?? string.Empty;

        public static unsafe void SetViewModel(CCSPlayerController player, string model)
        {
            ViewModel(player)?.SetModel(model);
        }

        public static void UpdateModel(CCSPlayerController player, CBasePlayerWeapon weapon, string model, string? worldModel, bool update)
        {
            weapon.Globalname = $"{GetViewModel(player)},{model},{worldModel}";
            weapon.SetModel(!string.IsNullOrEmpty(worldModel) ? worldModel : model);

            if (update)
            {
                SetViewModel(player, model);
            }
        }

        public static void ResetWeapon(CCSPlayerController player, CBasePlayerWeapon weapon, bool update)
        {
            string globalName = weapon.Globalname;
            if (string.IsNullOrEmpty(globalName)) return;

            string oldModel = GetFromGlobalName(globalName, GlobalNameData.ViewModelDefault);
            weapon.Globalname = string.Empty;
            weapon.SetModel(oldModel);

            if (update)
            {
                SetViewModel(player, oldModel);
            }
        }

        public static bool HandleEquip(CCSPlayerController player, Dictionary<string, string> item, bool isEquip)
        {
            if (player.PawnIsAlive)
            {
                CBasePlayerWeapon? weapon = Get(player, item["weapon"]);
                if (weapon != null)
                {
                    bool equip = weapon == player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

                    if (isEquip)
                    {
                        item.TryGetValue("worldmodel", out string? worldModel);
                        UpdateModel(player, weapon, item["viewmodel"], worldModel, equip);
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
            CPlayer_WeaponServices? weaponServices = player.PlayerPawn?.Value?.WeaponServices;
            if (weaponServices == null) return null;

            CBasePlayerWeapon? activeWeapon = weaponServices.ActiveWeapon?.Value;
            return activeWeapon != null && GetDesignerName(activeWeapon) == weaponName
                ? activeWeapon
                : (weaponServices.MyWeapons.SingleOrDefault(p => p.Value != null && GetDesignerName(p.Value) == weaponName)?.Value);
        }

        private static unsafe CBaseViewModel? ViewModel(CCSPlayerController player)
        {
            nint? handle = player.PlayerPawn.Value?.ViewModelServices?.Handle;
            if (handle == null || !handle.HasValue) return null;

            CCSPlayer_ViewModelServices viewModelServices = new(handle.Value);
            nint ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
            Span<nint> viewModels = MemoryMarshal.CreateSpan(ref ptr, 3);

            return new CHandle<CBaseViewModel>(viewModels[0]).Value;
        }
    }

    public static void Inspect(CCSPlayerController player, string model, string weapon)
    {
        if (player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value is not CBasePlayerWeapon activeWeapon) return;

        if (Weapon.GetDesignerName(activeWeapon) != weapon)
        {
            player.PrintToChatMessage("You need correct weapon", weapon);
            return;
        }

        string globalName = activeWeapon.Globalname;
        string oldModel = !string.IsNullOrEmpty(globalName) ? Weapon.GetFromGlobalName(globalName, Weapon.GlobalNameData.ViewModel) : Weapon.GetViewModel(player);

        Weapon.SetViewModel(player, model);

        Instance.AddTimer(3.0f, () =>
        {
            if (player.IsValid && player.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value == activeWeapon)
            {
                Weapon.SetViewModel(player, oldModel);
            }
        });
    }
}