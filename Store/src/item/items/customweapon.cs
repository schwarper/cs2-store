using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Store.Extension;
using System.Globalization;
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

    public void OnMapStart()
    {
        Weapon.ClearSubclassStates();
    }

    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
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

    public static void OnEntityDeleted(CEntityInstance entity)
    {
        Weapon.ForgetSubclassState(entity.Handle);
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
        if (itemData == null) return;

        if (!Weapon.TryParseWeaponSpec(itemData["weapon"], out string weaponBase, out string weaponSubclass))
        {
            return;
        }

        if (!weaponDesignerName.Equals(weaponBase, StringComparison.Ordinal)) return;

        try
        {
            ApplyModelToEntity(entity, entityType, player, weaponBase, weaponSubclass);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to set model for {entity.DesignerName}: {ex.Message}");
        }
    }

    private static void ApplyModelToEntity(CEntityInstance entity, EntityType entityType,
        CCSPlayerController player, string weaponBase, string weaponSubclass)
    {
        switch (entityType)
        {
            case EntityType.Weapon:
                CBasePlayerWeapon weapon = entity.As<CBasePlayerWeapon>();
                if (weapon?.IsValid != true || weapon.OriginalOwnerXuidLow <= 0) return;

                string baseSubclass = Weapon.ResolveBaseSubclass(weapon, weaponBase, player.TeamNum);
                Weapon.SetSubclass(weapon, weaponBase, baseSubclass, weaponSubclass);
                break;

            case EntityType.Projectile:
                break;
        }
    }

    public HookResult OnItemEquip(EventItemEquip @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (player == null) return HookResult.Continue;

        CBasePlayerWeapon? activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon?.IsValid != true) return HookResult.Continue;

        Weapon.FinishInactivePreviews(player, activeWeapon);

        List<StoreApi.Store.Store_Equipment> playerEquipments = Item.GetPlayerEquipments(player, "customweapon");
        foreach (StoreApi.Store.Store_Equipment equipment in playerEquipments)
        {
            Dictionary<string, string>? itemData = Item.GetItem(equipment.UniqueId);
            if (itemData == null) continue;

            if (!Weapon.TryParseWeaponSpec(itemData["weapon"], out string weaponBase, out string weaponSubclass))
            {
                continue;
            }

            if (!Weapon.MatchesWeapon(activeWeapon, weaponBase, weaponSubclass)) continue;

            string baseSubclass = Weapon.ResolveBaseSubclass(activeWeapon, weaponBase, player.TeamNum);
            Weapon.SetSubclass(activeWeapon, weaponBase, baseSubclass, weaponSubclass);
            break;
        }

        return HookResult.Continue;
    }

    public class Weapon
    {
        // Summary: Legacy viewmodel/worldmodel swapping is disabled; animgraph2 uses subclass changes instead.
        /*
        public enum GlobalNameData
        {
            ViewModelDefault,
            ViewModel,
            WorldModel
        }
        */

        private sealed class SubclassState(string weaponName, string baseSubclass)
        {
            public string WeaponName { get; } = weaponName;
            public string BaseSubclass { get; } = baseSubclass;
            public string? EquippedSubclass { get; set; }
            public string AppliedSubclass { get; set; } = string.Empty;
            public long OperationId { get; set; }
        }

        private static readonly Dictionary<nint, SubclassState> SubclassStates = new();
        private const float InspectPreviewDuration = 3.0f;
        private const int MaxGraphWaitUpdates = 16;
        private static readonly Dictionary<string, ushort> DefaultDefinitionIndexes = new(StringComparer.Ordinal)
        {
            ["weapon_deagle"] = 1,
            ["weapon_elite"] = 2,
            ["weapon_fiveseven"] = 3,
            ["weapon_glock"] = 4,
            ["weapon_ak47"] = 7,
            ["weapon_aug"] = 8,
            ["weapon_awp"] = 9,
            ["weapon_famas"] = 10,
            ["weapon_g3sg1"] = 11,
            ["weapon_galilar"] = 13,
            ["weapon_m249"] = 14,
            ["weapon_m4a1"] = 16,
            ["weapon_mac10"] = 17,
            ["weapon_p90"] = 19,
            ["weapon_mp7"] = 33,
            ["weapon_mp5sd"] = 23,
            ["weapon_ump45"] = 24,
            ["weapon_xm1014"] = 25,
            ["weapon_bizon"] = 26,
            ["weapon_mag7"] = 27,
            ["weapon_negev"] = 28,
            ["weapon_sawedoff"] = 29,
            ["weapon_tec9"] = 30,
            ["weapon_taser"] = 31,
            ["weapon_hkp2000"] = 32,
            ["weapon_mp9"] = 34,
            ["weapon_nova"] = 35,
            ["weapon_p250"] = 36,
            ["weapon_scar20"] = 38,
            ["weapon_sg556"] = 39,
            ["weapon_ssg08"] = 40,
            ["weapon_flashbang"] = 43,
            ["weapon_hegrenade"] = 44,
            ["weapon_smokegrenade"] = 45,
            ["weapon_molotov"] = 46,
            ["weapon_decoy"] = 47,
            ["weapon_incgrenade"] = 48,
            ["weapon_c4"] = 49,
            ["weapon_healthshot"] = 57,
            ["weapon_m4a1_silencer"] = 60,
            ["weapon_usp_silencer"] = 61,
            ["weapon_cz75a"] = 63,
            ["weapon_revolver"] = 64
        };
        private static long _nextOperationId;

        public static string GetDesignerName(CBasePlayerWeapon weapon)
        {
            string weaponDesignerName = weapon.DesignerName;
            ushort weaponIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;

            return (weaponDesignerName, weaponIndex) switch
            {
                var (name, _) when name.Contains("bayonet") => "weapon_knife",
                ("weapon_deagle", 64) => "weapon_revolver",
                ("weapon_m4a1", 60) => "weapon_m4a1_silencer",
                ("weapon_hkp2000", 61) => "weapon_usp_silencer",
                ("weapon_mp7", 23) => "weapon_mp5sd",
                _ => weaponDesignerName
            };
        }

        public static bool TryParseWeaponSpec(string weaponSpec, out string weaponName, out string weaponSubclass)
        {
            weaponName = string.Empty;
            weaponSubclass = string.Empty;

            if (string.IsNullOrEmpty(weaponSpec))
            {
                return false;
            }

            string[] parts = weaponSpec.Split(':', 2);
            weaponName = parts[0].Trim();
            weaponSubclass = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            return !string.IsNullOrEmpty(weaponName) && !string.IsNullOrEmpty(weaponSubclass);
        }

        public static string ResolveBaseSubclass(CBasePlayerWeapon weapon, string fallback, int teamNum)
        {
            if (fallback == "weapon_knife")
            {
                return teamNum == (int)CsTeam.Terrorist ? "59" : "42";
            }

            if (DefaultDefinitionIndexes.TryGetValue(fallback, out ushort defaultDefinitionIndex))
            {
                return defaultDefinitionIndex.ToString(CultureInfo.InvariantCulture);
            }

            ushort definitionIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;
            return definitionIndex > 0
                ? definitionIndex.ToString(CultureInfo.InvariantCulture)
                : fallback;
        }

        /*
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
        */

        public static bool HandleEquip(CCSPlayerController player, Dictionary<string, string> item, bool isEquip)
        {
            if (player.PawnIsAlive)
            {
                if (!TryParseWeaponSpec(item["weapon"], out string weaponBase, out string weaponSubclass))
                {
                    return true;
                }

                CBasePlayerWeapon? weapon = Get(player, weaponBase, weaponSubclass);
                if (weapon != null)
                {
                    string baseSubclass = ResolveBaseSubclass(weapon, weaponBase, player.TeamNum);
                    if (isEquip)
                    {
                        SetSubclass(weapon, weaponBase, baseSubclass, weaponSubclass);
                    }
                    else
                    {
                        ResetSubclass(weapon, weaponBase, baseSubclass);
                    }
                }
            }

            return true;
        }

        private static CBasePlayerWeapon? Get(CCSPlayerController player, string weaponName, string weaponSubclass)
        {
            CPlayer_WeaponServices? weaponServices = player.PlayerPawn?.Value?.WeaponServices;
            if (weaponServices == null) return null;

            CBasePlayerWeapon? activeWeapon = weaponServices.ActiveWeapon?.Value;
            return activeWeapon?.IsValid == true && MatchesWeapon(activeWeapon, weaponName, weaponSubclass)
                ? activeWeapon
                : weaponServices.MyWeapons.FirstOrDefault(p => p.Value?.IsValid == true && MatchesWeapon(p.Value, weaponName, weaponSubclass))?.Value;
        }

        public static bool MatchesWeapon(CBasePlayerWeapon weapon, string weaponName, string? weaponSubclass = null)
        {
            if (SubclassStates.TryGetValue(weapon.Handle, out SubclassState? state))
            {
                return string.Equals(state.WeaponName, weaponName, StringComparison.Ordinal);
            }

            string designerName = GetDesignerName(weapon);
            return string.Equals(designerName, weaponName, StringComparison.Ordinal) ||
                   (!string.IsNullOrEmpty(weaponSubclass) &&
                    string.Equals(designerName, weaponSubclass, StringComparison.Ordinal)) ||
                   designerName.StartsWith(weaponName + "+", StringComparison.Ordinal);
        }

        /*
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
        */

        // Animgraph2 custom weapons are VData subclasses. Keep the original subclass
        // stable for the complete lifetime of the entity: equip and inspect can overlap.
        public static void SetSubclass(CBasePlayerWeapon weapon, string weaponName, string oldSubclass, string newSubclass)
        {
            if (!weapon.IsValid || string.IsNullOrWhiteSpace(oldSubclass) || string.IsNullOrWhiteSpace(newSubclass))
            {
                return;
            }

            var handle = weapon.Handle;
            SubclassState state = GetOrCreateSubclassState(handle, weaponName, oldSubclass);
            state.EquippedSubclass = newSubclass;
            long operationId = BeginOperation(state);
            ApplySubclassSafely(weapon, state, newSubclass, operationId);
        }

        public static void ResetSubclass(CBasePlayerWeapon weapon, string weaponName, string fallbackBaseSubclass)
        {
            if (!weapon.IsValid || string.IsNullOrWhiteSpace(fallbackBaseSubclass))
            {
                return;
            }

            SubclassState state = GetOrCreateSubclassState(weapon.Handle, weaponName, fallbackBaseSubclass);
            state.EquippedSubclass = null;
            long operationId = BeginOperation(state);
            ApplySubclassSafely(weapon, state, state.BaseSubclass, operationId, true);
        }

        public static long PreviewSubclass(CBasePlayerWeapon weapon, string weaponName, string oldSubclass,
            string previewSubclass, bool keepPreviewEquipped)
        {
            if (!weapon.IsValid || string.IsNullOrWhiteSpace(oldSubclass) || string.IsNullOrWhiteSpace(previewSubclass))
            {
                return 0;
            }

            SubclassState state = GetOrCreateSubclassState(weapon.Handle, weaponName, oldSubclass);
            if (keepPreviewEquipped)
            {
                state.EquippedSubclass = previewSubclass;
            }

            long operationId = BeginOperation(state);
            ApplySubclassSafely(weapon, state, previewSubclass, operationId, afterApply: () =>
                Instance.AddTimer(InspectPreviewDuration, () => FinishPreview(weapon, operationId)));
            return operationId;
        }

        public static void FinishPreview(CBasePlayerWeapon weapon, long operationId)
        {
            if (operationId == 0 || !weapon.IsValid ||
                !SubclassStates.TryGetValue(weapon.Handle, out SubclassState? state) ||
                state.OperationId != operationId)
            {
                return;
            }

            RestorePreview(weapon, state);
        }

        public static void FinishInactivePreviews(CCSPlayerController player, CBasePlayerWeapon activeWeapon)
        {
            CPlayer_WeaponServices? weaponServices = player.PlayerPawn?.Value?.WeaponServices;
            if (weaponServices == null)
            {
                return;
            }

            foreach (CHandle<CBasePlayerWeapon> weaponHandle in weaponServices.MyWeapons)
            {
                CBasePlayerWeapon? weapon = weaponHandle.Value;
                if (weapon?.IsValid != true || weapon.Handle == activeWeapon.Handle ||
                    !SubclassStates.TryGetValue(weapon.Handle, out SubclassState? state) ||
                    state.EquippedSubclass != null)
                {
                    continue;
                }

                RestorePreview(weapon, state);
            }
        }

        private static void RestorePreview(CBasePlayerWeapon weapon, SubclassState state)
        {
            string restoreSubclass = state.EquippedSubclass ?? state.BaseSubclass;
            long restoreOperationId = BeginOperation(state);
            ApplySubclassSafely(
                weapon,
                state,
                restoreSubclass,
                restoreOperationId,
                state.EquippedSubclass == null);
        }

        public static void ForgetSubclassState(nint handle)
        {
            SubclassStates.Remove(handle);
        }

        public static void ClearSubclassStates()
        {
            SubclassStates.Clear();
        }

        private static SubclassState GetOrCreateSubclassState(nint handle, string weaponName, string baseSubclass)
        {
            if (!SubclassStates.TryGetValue(handle, out SubclassState? state))
            {
                state = new SubclassState(weaponName, baseSubclass);
                SubclassStates[handle] = state;
            }

            return state;
        }

        private static long NextOperationId()
        {
            return ++_nextOperationId;
        }

        private static long BeginOperation(SubclassState state)
        {
            long operationId = NextOperationId();
            state.OperationId = operationId;
            return operationId;
        }

        private static void ApplySubclassSafely(CBasePlayerWeapon weapon, SubclassState state,
            string subclass, long operationId, bool removeStateAfterApply = false, Action? afterApply = null,
            int graphWaitUpdates = 0, bool deferToWorldUpdate = true)
        {
            if (deferToWorldUpdate)
            {
                Server.NextWorldUpdate(() =>
                    ApplySubclassSafely(
                        weapon,
                        state,
                        subclass,
                        operationId,
                        removeStateAfterApply,
                        afterApply,
                        graphWaitUpdates,
                        false));
                return;
            }

            if (!weapon.IsValid ||
                !SubclassStates.TryGetValue(weapon.Handle, out SubclassState? currentState) ||
                !ReferenceEquals(currentState, state) ||
                state.OperationId != operationId)
            {
                return;
            }

            if (string.IsNullOrEmpty(state.AppliedSubclass) && IsWeaponAlreadyUsingSubclass(weapon, subclass))
            {
                state.AppliedSubclass = subclass;
            }

            if (string.Equals(state.AppliedSubclass, subclass, StringComparison.Ordinal))
            {
                CompleteSubclassOperation(weapon, state, operationId, removeStateAfterApply, afterApply);
                return;
            }

            if (IsInspectActive(weapon))
            {
                Server.NextWorldUpdate(() =>
                    ApplySubclassSafely(
                        weapon,
                        state,
                        subclass,
                        operationId,
                        removeStateAfterApply,
                        afterApply,
                        graphWaitUpdates,
                        false));
                return;
            }

            if (IsInspectBlockedUntilGraphUpdate(weapon) && graphWaitUpdates < MaxGraphWaitUpdates)
            {
                Server.NextWorldUpdate(() =>
                    ApplySubclassSafely(
                        weapon,
                        state,
                        subclass,
                        operationId,
                        removeStateAfterApply,
                        afterApply,
                        graphWaitUpdates + 1,
                        false));
                return;
            }

            BlockInspectUntilGraphUpdate(weapon);
            weapon.InitiallyPopulateInterpHistory = true;
            weapon.AcceptInput("ChangeSubclass", weapon, weapon, subclass);
            state.AppliedSubclass = subclass;
            CompleteSubclassOperation(weapon, state, operationId, removeStateAfterApply, afterApply);
        }

        private static void CompleteSubclassOperation(CBasePlayerWeapon weapon, SubclassState state,
            long operationId, bool removeStateAfterApply, Action? afterApply)
        {
            afterApply?.Invoke();

            if (removeStateAfterApply && state.OperationId == operationId)
            {
                SubclassStates.Remove(weapon.Handle);
            }
        }

        private static bool IsWeaponAlreadyUsingSubclass(CBasePlayerWeapon weapon, string subclass)
        {
            if (ushort.TryParse(subclass, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out ushort definitionIndex))
            {
                return weapon.AttributeManager.Item.ItemDefinitionIndex == definitionIndex;
            }

            return string.Equals(weapon.DesignerName, subclass, StringComparison.Ordinal) ||
                   string.Equals(GetDesignerName(weapon), subclass, StringComparison.Ordinal);
        }

        private static bool IsInspectActive(CBasePlayerWeapon weapon)
        {
            CCSWeaponBase csWeapon = weapon.As<CCSWeaponBase>();
            if (!csWeapon.IsValid)
            {
                return false;
            }

            WeaponGameplayAnimState animationState = csWeapon.WeaponGameplayAnimState;
            return csWeapon.InspectPending ||
                   csWeapon.InspectCancelCompleteTime > Server.CurrentTime ||
                   animationState == WeaponGameplayAnimState.WPN_ANIMSTATE_INSPECT ||
                   animationState == WeaponGameplayAnimState.WPN_ANIMSTATE_INSPECT_OUTRO;
        }

        private static bool IsInspectBlockedUntilGraphUpdate(CBasePlayerWeapon weapon)
        {
            return TryGetWeaponGraphServices(weapon, out _, out CCSPlayer_WeaponServices weaponServices) &&
                   weaponServices.BlockInspectUntilNextGraphUpdate;
        }

        private static void BlockInspectUntilGraphUpdate(CBasePlayerWeapon weapon)
        {
            if (!TryGetWeaponGraphServices(
                    weapon,
                    out CCSPlayerPawn pawn,
                    out CCSPlayer_WeaponServices weaponServices) ||
                weaponServices.BlockInspectUntilNextGraphUpdate)
            {
                return;
            }

            weaponServices.BlockInspectUntilNextGraphUpdate = true;
            Utilities.SetStateChanged(
                pawn,
                "CCSPlayer_WeaponServices",
                "m_bBlockInspectUntilNextGraphUpdate");
        }

        private static bool TryGetWeaponGraphServices(CBasePlayerWeapon weapon, out CCSPlayerPawn pawn,
            out CCSPlayer_WeaponServices weaponServices)
        {
            pawn = null!;
            weaponServices = null!;

            CCSPlayerPawn? ownerPawn = FindTarget.FindTargetFromWeapon(weapon)?.PlayerPawn.Value;
            CPlayer_WeaponServices? baseWeaponServices = ownerPawn?.WeaponServices;
            if (ownerPawn?.IsValid != true || baseWeaponServices == null)
            {
                return false;
            }

            pawn = ownerPawn;
            weaponServices = baseWeaponServices.As<CCSPlayer_WeaponServices>();
            return weaponServices != null;
        }

        /*
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
        */
    }

    public static void Inspect(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value is not CBasePlayerWeapon activeWeapon) return;

        if (!item.TryGetValue("weapon", out string? weapon) ||
            !Weapon.TryParseWeaponSpec(weapon, out string weaponBase, out string weaponSubclass))
        {
            return;
        }

        if (!Weapon.MatchesWeapon(activeWeapon, weaponBase, weaponSubclass))
        {
            player.PrintToChatMessage("You need correct weapon", weaponBase);
            return;
        }

        string baseSubclass = Weapon.ResolveBaseSubclass(activeWeapon, weaponBase, player.TeamNum);
        bool itemIsEquipped = Item.PlayerUsing(player, item["type"], item["uniqueid"]);
        Weapon.PreviewSubclass(activeWeapon, weaponBase, baseSubclass, weaponSubclass, itemIsEquipped);
    }
}
