using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using System.Runtime.InteropServices;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

[StoreItemType("customweapon")]
public class Item_CustomWeapon : IItemModule
{
    public bool Equipable => true;
    public bool? RequiresAlive => null;

    private static bool _customWeaponExists = false;
    private enum EntityType
    {
        None,
        Weapon,
        Projectile
    }

    public void OnPluginStart()
    {
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

    public void OnMapStart() { }

    public void OnServerPrecacheResources(ResourceManifest manifest)
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

        if (entity.DesignerName.EndsWith("_projectile"))
        {
            entityType = EntityType.Projectile;
            return true;
        }

        return false;
    }

    private static void ProcessEntity(CEntityInstance entity, EntityType entityType)
    {
        CCSPlayerController? player = GetPlayerFromEntity(entity, entityType);
        if (player == null) return;

        List<StoreApi.Store.Store_Equipment> playerEquipments = Item.GetPlayerEquipments(player, "customweapon");
        if (playerEquipments.Count == 0) return;

        string weaponDesignerName = GetWeaponDesignerName(entity, entityType);

        foreach (StoreApi.Store.Store_Equipment equipment in playerEquipments)
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
                if (weapon?.IsValid == true && weapon.OriginalOwnerXuidLow > 0)
                {
                    return FindTarget.FindTargetFromWeapon(weapon);
                }
                break;

            case EntityType.Projectile:
                CBaseCSGrenadeProjectile projectile = entity.As<CBaseCSGrenadeProjectile>();
                return projectile?.OriginalThrower?.Value?.OriginalController.Value;
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

    private static void TryApplyEquipmentModel(CEntityInstance entity, StoreApi.Store.Store_Equipment equipment,
        string weaponDesignerName, EntityType entityType, CCSPlayerController player)
    {
        Dictionary<string, string>? itemData = Item.GetItem(equipment.UniqueId);
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
                if (weapon?.IsValid != true || weapon.OriginalOwnerXuidLow <= 0) return;

                CBasePlayerWeapon? activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
                Weapon.UpdateModel(player, weapon, viewModel, worldModel, weapon == activeWeapon);
                break;

            case EntityType.Projectile:
                CBaseCSGrenadeProjectile projectile = entity.As<CBaseCSGrenadeProjectile>();
                if (projectile?.IsValid == true)
                {
                    projectile.SetModel(worldModel);
                }
                break;
        }
    }

    public HookResult OnItemEquip(EventItemEquip @event, GameEventInfo info)
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

    public class Weapon
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

        public static string GetViewModel(CCSPlayerController player)
        {
            var entity = ViewModel(player);
            if (entity == null || !entity.IsValid)
                return string.Empty;

            int modelOffset = Schema.GetSchemaOffset("CBaseEntity", "m_ModelName");
            if (modelOffset == 0)
                return string.Empty;

            var modelPtr = Marshal.ReadIntPtr(entity.Handle + modelOffset);
            if (modelPtr == IntPtr.Zero)
                return string.Empty;

            return Marshal.PtrToStringAnsi(modelPtr) ?? string.Empty;
        }

        public static void SetViewModel(CCSPlayerController player, string model)
        {
            var entity = ViewModel(player);
            if (entity == null || !entity.IsValid)
                return;

            var modelPtr = Marshal.StringToHGlobalAnsi(model);

            int offset = GameData.GetOffset("CBaseModelEntity_SetModel");
            if (offset == 0)

                VirtualFunction.CreateVoid<nint, nint>(entity.Handle, offset)(entity.Handle, modelPtr);

            Marshal.FreeHGlobal(modelPtr);
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

        private static CBaseEntity? ViewModel(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                return null;

            int offset = Schema.GetSchemaOffset("CBasePlayer", "m_hViewModel");
            if (offset == 0)
                return null;

            var handle = Marshal.ReadIntPtr(pawn.Handle + offset);
            if (handle == IntPtr.Zero)
                return null;

            return new CHandle<CBaseEntity>(handle).Value;
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