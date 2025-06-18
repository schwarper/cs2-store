using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

[StoreItemType("customweapon")]
public abstract class ItemCustomWeapon : IItemModule
{
    public bool Equipable => true;
    public bool? RequiresAlive => null;

    private static bool _customWeaponExists;
    private enum EntityType
    {
        None,
        Weapon,
        Projectile
    }

    public void OnPluginStart()
    {
        if (!Item.IsAnyItemExistInType("customweapon"))
            return;
        
        if (CoreConfig.FollowCS2ServerGuidelines)
        {
            throw new Exception("Cannot set or get 'CEconEntity::m_OriginalOwnerXuidLow' with \"FollowCS2ServerGuidelines\" option enabled.");
        }

        Instance.RegisterEventHandler<EventItemEquip>(OnItemEquip);
        _customWeaponExists = true;
    }

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        var items = Item.GetItemsByType("customweapon");

        foreach (var item in items)
        {
            manifest.AddResource(item.Value["viewmodel"]);

            if (item.Value.TryGetValue("worldmodel", out string? worldmodel) && !string.IsNullOrEmpty(worldmodel))
            {
                manifest.AddResource(worldmodel);
            }
        }
    }

    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return Weapon.HandleEquip(player, item, true);
    }

    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return !update || Weapon.HandleEquip(player, item, false);
    }

    public static void OnEntityCreated(CEntityInstance entity)
    {
        if (!_customWeaponExists) return;

        if (!IsRelevantEntity(entity, out EntityType entityType)) return;

        Server.NextWorldUpdate(() => ProcessEntity(entity, entityType));
    }

    private static bool IsRelevantEntity(CEntityInstance entity, out EntityType entityType)
    {
        entityType = EntityType.None;

        if (entity.DesignerName.StartsWith("weapon_"))
        {
            entityType = EntityType.Weapon;
            return true;
        }

        if (!entity.DesignerName.EndsWith("_projectile"))
            return false;
        
        entityType = EntityType.Projectile;
        return true;

    }

    private static void ProcessEntity(CEntityInstance entity, EntityType entityType)
    {
        CCSPlayerController? player = GetPlayerFromEntity(entity, entityType);
        if (player == null) return;

        var playerEquipments = Item.GetPlayerEquipments(player, "customweapon");
        if (playerEquipments.Count == 0) return;

        string weaponDesignerName = GetWeaponDesignerName(entity, entityType);

        foreach (StoreEquipment equipment in playerEquipments)
        {
            TryApplyEquipmentModel(entity, equipment, weaponDesignerName, entityType, player);
        }
    }

    private static CCSPlayerController? GetPlayerFromEntity(CEntityInstance entity, EntityType entityType)
    {
        switch (entityType)
        {
            case EntityType.Weapon:
                CBasePlayerWeapon weapon = new(entity.Handle);
                if (weapon is { IsValid: true, OriginalOwnerXuidLow: > 0 })
                {
                    return FindTarget.FindTargetFromWeapon(weapon);
                }
                break;

            case EntityType.Projectile:
                CBaseCSGrenadeProjectile projectile = entity.As<CBaseCSGrenadeProjectile>();
                return projectile.OriginalThrower.Value?.OriginalController.Value;
            case EntityType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(entityType), entityType, null);
        }

        return null;
    }

    private static string GetWeaponDesignerName(CEntityInstance entity, EntityType entityType)
    {
        return entityType switch
        {
            EntityType.Weapon => Weapon.GetDesignerName(entity.As<CBasePlayerWeapon>()),
            EntityType.Projectile => "weapon_" + entity.DesignerName.Replace("_projectile", ""),
            _ => string.Empty
        };
    }

    private static void TryApplyEquipmentModel(CEntityInstance entity, StoreEquipment equipment,
        string weaponDesignerName, EntityType entityType, CCSPlayerController player)
    {
        var itemData = Item.GetItem(equipment.UniqueId);
        if (itemData == null || !weaponDesignerName.Contains(itemData["weapon"])) return;

        itemData.TryGetValue("worldmodel", out string? worldModel);
        worldModel = string.IsNullOrEmpty(worldModel) ? itemData["viewmodel"] : worldModel;

        try
        {
            ApplyModelToEntity(entity, entityType, player, itemData["viewmodel"], worldModel);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to set model for {entity.DesignerName}: {ex.Message}");
        }
    }

    private static void ApplyModelToEntity(CEntityInstance entity, EntityType entityType,
        CCSPlayerController player, string viewModel, string worldModel)
    {
        switch (entityType)
        {
            case EntityType.Weapon:
                CBasePlayerWeapon weapon = entity.As<CBasePlayerWeapon>();
                if (weapon.IsValid != true || weapon.OriginalOwnerXuidLow <= 0) return;

                CBasePlayerWeapon? activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
                Weapon.UpdateModel(player, weapon, viewModel, worldModel, weapon == activeWeapon);
                break;

            case EntityType.Projectile:
                CBaseCSGrenadeProjectile projectile = entity.As<CBaseCSGrenadeProjectile>();
                if (projectile.IsValid)
                {
                    projectile.SetModel(worldModel);
                }
                break;
            case EntityType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(entityType), entityType, null);
        }
    }

    private static HookResult OnItemEquip(EventItemEquip @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null) return HookResult.Continue;

        string? globalName = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value?.Globalname;
        if (string.IsNullOrEmpty(globalName)) return HookResult.Continue;
        
        string model = Weapon.GetFromGlobalName(globalName, Weapon.GlobalNameData.ViewModel);
        Weapon.SetViewModel(player, model);

        return HookResult.Continue;
    }

    public abstract class Weapon
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

        public static string GetViewModel(CCSPlayerController player)
        {
            return ViewModel(player)?.VMName ?? string.Empty;
        }

        public static void SetViewModel(CCSPlayerController player, string model)
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

        private static void ResetWeapon(CCSPlayerController player, CBasePlayerWeapon weapon, bool update)
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
            if (!player.PawnIsAlive)
                return true;
            
            CBasePlayerWeapon? weapon = Get(player, item["weapon"]);
            if (weapon == null)
                return true;
            
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

            return true;
        }

        private static CBasePlayerWeapon? Get(CCSPlayerController player, string weaponName)
        {
            CPlayer_WeaponServices? weaponServices = player.PlayerPawn.Value?.WeaponServices;
            if (weaponServices == null) return null;

            CBasePlayerWeapon? activeWeapon = weaponServices.ActiveWeapon.Value;
            return activeWeapon != null && GetDesignerName(activeWeapon) == weaponName
                ? activeWeapon
                : (weaponServices.MyWeapons.SingleOrDefault(p => p.Value != null && GetDesignerName(p.Value) == weaponName)?.Value);
        }

        private static CBaseViewModel? ViewModel(CCSPlayerController player)
        {
            nint? handle = player.PlayerPawn.Value?.ViewModelServices?.Handle;
            if (!handle.HasValue) return null;

            CCSPlayer_ViewModelServices viewModelServices = new(handle.Value);
            nint ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
            var viewModels = MemoryMarshal.CreateSpan(ref ptr, 3);

            return new CHandle<CBaseViewModel>(viewModels[0]).Value;
        }
    }

    public static void Inspect(CCSPlayerController player, string model, string weapon)
    {
        if (player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value is not { } activeWeapon) return;

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