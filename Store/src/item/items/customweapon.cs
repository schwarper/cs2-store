using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using static Store.Store;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using System.Runtime.InteropServices;

namespace Store;

public static class Item_CustomWeapon
{
    private static bool customweaponExists = false;

    public static void OnPluginStart()
    {
        Item.RegisterType("customweapon", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        if (Item.IsAnyItemExistInType("customweapon"))
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

            if (item.Value.TryGetValue("worldmodel", out string? worldmodel) && !string.IsNullOrEmpty(worldmodel))
            {
                manifest.AddResource(worldmodel);
            }
        }
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return Weapon.HandleEquip(player, item, true);
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return !update || Weapon.HandleEquip(player, item, false);
    }

    public static void OnEntityCreated(CEntityInstance entity)
    {
        if (!customweaponExists || !entity.DesignerName.StartsWith("weapon_"))
        {
            return;
        }

        Server.NextWorldUpdate(() =>
        {
            CBasePlayerWeapon weapon = new(entity.Handle);

            if (!weapon.IsValid || weapon.OriginalOwnerXuidLow <= 0)
            {
                return;
            }

            SteamID? _steamid = new(weapon.OriginalOwnerXuidLow);
            CCSPlayerController? player = null;

            if (_steamid != null && _steamid.IsValid())
            {
                player =
                    Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == _steamid.SteamId64) ??
                    Utilities.GetPlayerFromSteamId(weapon.OriginalOwnerXuidLow);
            }
            else
            {
                CCSWeaponBaseGun gun = weapon.As<CCSWeaponBaseGun>();
                player = Utilities.GetPlayerFromIndex((int)weapon.OwnerEntity.Index) ?? Utilities.GetPlayerFromIndex((int)gun.OwnerEntity.Value!.Index);
            }

            if (string.IsNullOrEmpty(player?.PlayerName))
            {
                return;
            }

            CBasePlayerWeapon? activeweapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
            List<StoreApi.Store.Store_Equipment> playerequipments = [.. Item.GetPlayerEquipments(player).Where(p => p.SteamID == player.SteamID && p.Type == "customweapon")];

            foreach (StoreApi.Store.Store_Equipment? playerequipment in playerequipments)
            {
                Dictionary<string, string>? itemdata = Item.GetItem(playerequipment.UniqueId);

                if (itemdata == null)
                {
                    continue;
                }

                string weaponDesignerName = Weapon.GetDesignerName(weapon);

                if (!weaponDesignerName.Contains(itemdata["weapon"]))
                {
                    continue;
                }

                itemdata.TryGetValue("worldmodel", out string? worldmodel);
                Weapon.UpdateModel(player, weapon, itemdata["uniqueid"], worldmodel, weapon == activeweapon);
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
            string model = Weapon.GetFromGlobalName(globalname, Weapon.GlobalNameData.ViewModel);

            Weapon.SetViewModel(player, model, activeweapon, true);
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
        };
        public static string GetDesignerName(CBasePlayerWeapon weapon)
        {
            string weaponDesignerName = weapon.DesignerName;
            ushort weaponIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;

            weaponDesignerName = (weaponDesignerName, weaponIndex) switch
            {
                var (name, _) when name.Contains("bayonet") => "weapon_knife",
                ("weapon_m4a1", 60) => "weapon_m4a1_silencer",
                ("weapon_hkp2000", 61) => "weapon_usp_silencer",
                ("weapon_mp7", 23) => "weapon_mp5sd",
                _ => weaponDesignerName
            };

            return weaponDesignerName;
        }
        public static string GetFromGlobalName(string globalname, GlobalNameData data)
        {
            string[] globalnamesplit = globalname.Split(',');

            return data switch
            {
                GlobalNameData.ViewModelDefault => globalnamesplit[0],
                GlobalNameData.ViewModel => globalnamesplit[1],
                GlobalNameData.WorldModel => !string.IsNullOrEmpty(globalnamesplit[2]) ? globalnamesplit[2] : globalnamesplit[1],
                _ => throw new NotImplementedException(),
            };
        }
        public static unsafe string GetViewModel(CCSPlayerController player)
        {
            return ViewModel(player)?.VMName ?? string.Empty;
        }
        public static unsafe void SetViewModel(CCSPlayerController player, string model, CBasePlayerWeapon activeWeapon, bool updateDefaultWeapon)
        {
            ViewModel(player)?.SetModel(model);
            /*
            if (updateDefaultWeapon)
            {
                string defaultWeapon = GetViewModel(player);
                ViewModel(player)?.SetModel(defaultWeapon);

                Instance.AddTimer(0.1f, () =>
                {
                    if (player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value != activeWeapon)
                    {
                        return;
                    }

                    ViewModel(player)?.SetModel(model);
                });
            }
            else
            {
                ViewModel(player)?.SetModel(model);
            }
            */
        }
        public static void SetWorldModel(CBasePlayerWeapon weapon, string model)
        {
            weapon.SetModel(model);
        }
        public static void UpdateModel(CCSPlayerController player, CBasePlayerWeapon weapon, string model, string? worldmodel, bool update)
        {
            weapon.Globalname = $"{GetViewModel(player)},{model},{worldmodel}";

            weapon.SetModel(!string.IsNullOrEmpty(worldmodel) ? worldmodel : model);

            if (update)
            {
                SetViewModel(player, model, weapon, true);
            }
        }
        public static void ResetWeapon(CCSPlayerController player, CBasePlayerWeapon weapon, bool update)
        {
            string globalname = weapon.Globalname;

            if (string.IsNullOrEmpty(globalname))
            {
                return;
            }

            var oldmodel = GetFromGlobalName(globalname, GlobalNameData.ViewModelDefault);

            weapon.Globalname = string.Empty;
            weapon.SetModel(oldmodel);

            if (update)
            {
                SetViewModel(player, oldmodel, weapon, false);
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
                        item.TryGetValue("worldmodel", out string? worldmodel);
                        UpdateModel(player, weapon, item["uniqueid"], worldmodel, equip);
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

            if (weaponServices == null)
            {
                return null;
            }

            CBasePlayerWeapon? activeWeapon = weaponServices.ActiveWeapon?.Value;

            if (activeWeapon != null && GetDesignerName(activeWeapon) == weaponName)
            {
                return activeWeapon;
            }

            return weaponServices.MyWeapons.SingleOrDefault(p => p.Value != null && GetDesignerName(p.Value) == weaponName)?.Value;
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

    public static void Inspect(CCSPlayerController player, string model, string weapon)
    {
        if (player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value is not CBasePlayerWeapon activeWeapon)
        {
            return;
        }

        if (Weapon.GetDesignerName(activeWeapon) != weapon)
        {
            player.PrintToChatMessage("You need correct weapon", weapon);
            return;
        }

        string globalname = activeWeapon.Globalname;
        string oldModel;

        if (!string.IsNullOrEmpty(globalname))
        {
            oldModel = Weapon.GetFromGlobalName(globalname, Weapon.GlobalNameData.ViewModel);
        }
        else
        {
            oldModel = Weapon.GetViewModel(player);
        }

        Weapon.SetViewModel(player, model, activeWeapon, true);

        Instance.AddTimer(3.0f, () =>
        {
            if (player.IsValid && player.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value == activeWeapon)
            {
                Weapon.SetViewModel(player, oldModel, activeWeapon, true);
            }
        });
    }
}