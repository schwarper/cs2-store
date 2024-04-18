using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Runtime.InteropServices;
using static CounterStrikeSharp.API.Core.Listeners;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public class WeaponData
    {
        public required uint Index { get; set; }
        public required CCSPlayerController Controller { get; set; }
        public required string Model { get; set; }
        public required string OldModel { get; set; }
    }

    public List<WeaponData> CustomWeapons { get; set; } = [];

    public static void CustomWeapon_OnPluginStart()
    {
        Item.RegisterType("customweapon", OnMapStart, Equip, Unequip, true, null);

        Instance.RegisterEventHandler<EventItemEquip>(OnItemEquip);
    }

    public static void OnMapStart()
    {
        Instance.RegisterListener<OnServerPrecacheResources>((manifest) =>
        {
            List<KeyValuePair<string, Dictionary<string, string>>> items = Item.GetItemsByType("customweapon");

            foreach (KeyValuePair<string, Dictionary<string, string>> item in items)
            {
                manifest.AddResource(item.Value["uniqueid"]);
            }
        });
    }

    public static bool Equip(CCSPlayerController player, Dictionary<string, string> item)
    {
        CBasePlayerWeapon? activeweapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

        if (activeweapon != null && activeweapon.DesignerName.Contains(item["weapon"]))
        {
            string oldmodel = GetViewModel(player);

            CreateCustomModel(activeweapon, player, item["uniqueid"], oldmodel, true);
        }
        return true;
    }

    public static bool Unequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        CBasePlayerWeapon? activeweapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

        if (activeweapon != null && activeweapon.DesignerName.Contains(item["weapon"]))
        {
            RemoveCustomModel(activeweapon, player, true);
        }
        return true;
    }

    public static void OnEntityCreated_CustomWeapon(CEntityInstance entity)
    {
        if (!entity.DesignerName.StartsWith("weapon_"))
        {
            return;
        }

        Server.NextFrame(() =>
        {
            CBasePlayerWeapon? weapon = new(entity.Handle);

            if (weapon == null)
            {
                return;
            }

            CHandle<CBaseEntity> ownerHandle = weapon.OwnerEntity;

            if (ownerHandle == null)
            {
                return;
            }

            CCSPlayerController? player = ownerHandle.Value?.As<CCSPlayerController>();

            if (player == null)
            {
                return;
            }

            if (player == null || !player.IsValid)
            {
                return;
            }

            Store_Equipment? playerequipment = Item.GetPlayerEquipments(player).FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "customweapon");

            if (playerequipment == null)
            {
                return;
            }

            Dictionary<string, string>? itemdata = Item.GetItem(playerequipment.Type, playerequipment.UniqueId);

            if (itemdata == null)
            {
                return;
            }

            if (!weapon.DesignerName.Contains(itemdata["weapon"]))
            {
                return;
            }

            string oldmodel = GetViewModel(player);

            CreateCustomModel(weapon, player, itemdata["uniqueid"], oldmodel, false);
        });
    }

    public static HookResult OnItemEquip(EventItemEquip @event, GameEventInfo info)
    {
        CCSPlayerController player = @event.Userid;

        if (player == null || !player.IsValid)
        {
            return HookResult.Continue;
        }

        CBasePlayerWeapon activeweapon = player.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value!;

        Server.NextFrame(() =>
        {
            WeaponData? data = Instance.CustomWeapons.FirstOrDefault(p => p.Index == activeweapon.Index);

            if (data == null)
            {
                return;
            }

            ChangeModel(player, data.Model);
        });

        return HookResult.Continue;
    }

    public static void CreateCustomModel(CBasePlayerWeapon weapon, CCSPlayerController player, string model, string oldmodel, bool update)
    {
        weapon.SetModel(model);

        Instance.CustomWeapons.Add(new WeaponData
        {
            Controller = player,
            Index = weapon.Index,
            Model = model,
            OldModel = oldmodel
        });

        if (update)
        {
            ChangeModel(player, model);
        }
    }

    public static void RemoveCustomModel(CBasePlayerWeapon weapon, CCSPlayerController player, bool update)
    {
        WeaponData? rweapon = Instance.CustomWeapons.FirstOrDefault(p => p.Index == weapon.Index && p.Controller == player);

        if (rweapon == null)
        {
            return;
        }

        weapon.SetModel(rweapon.OldModel);

        if (update)
        {
            ChangeModel(player, rweapon.OldModel);
        }

        if (rweapon != null)
        {
            Instance.CustomWeapons.Remove(rweapon);
        }
    }

    public static unsafe void ChangeModel(CCSPlayerController player, string path)
    {
        CCSPlayer_ViewModelServices viewModelServices = new(player.PlayerPawn.Value!.ViewModelServices!.Handle);

        nint ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
        Span<nint> viewModels = MemoryMarshal.CreateSpan(ref ptr, 3);

        CHandle<CBaseViewModel> viewModel = new(viewModels[0]);

        viewModel.Value!.SetModel(path);
    }

    public static unsafe string GetViewModel(CCSPlayerController player)
    {
        CCSPlayer_ViewModelServices viewModelServices = new(player.PlayerPawn.Value!.ViewModelServices!.Handle);

        nint ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
        Span<nint> viewModels = MemoryMarshal.CreateSpan(ref ptr, 3);

        CHandle<CBaseViewModel> viewModel = new(viewModels[0]);

        return viewModel.Value!.VMName;
    }
}