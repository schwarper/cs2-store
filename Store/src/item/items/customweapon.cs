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
            if (weapon.OriginalOwnerXuidLow == 0)
            {
                return;
            }

            CCSPlayerController? player = Utilities.GetPlayerFromSteamId(weapon.OriginalOwnerXuidLow);

            if (player == null)
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

            CBasePlayerWeapon? activeweapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

            if (activeweapon != null && weapon == activeweapon)
            {
                Weapon.UpdateModel(player, activeweapon, itemdata["uniqueid"], true);
            }
            else
            {
                Weapon.UpdateModel(player, weapon, itemdata["uniqueid"], false);
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
}

public class Weapon
{
    public static unsafe CBaseViewModel ViewModel(CCSPlayerController player)
    {
        CCSPlayer_ViewModelServices viewModelServices = new(player.PlayerPawn.Value!.ViewModelServices!.Handle);

        nint ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
        Span<nint> viewModels = MemoryMarshal.CreateSpan(ref ptr, 3);

        CHandle<CBaseViewModel> viewModel = new(viewModels[0]);

        return viewModel.Value!;
    }
    public static unsafe string GetViewModel(CCSPlayerController player)
    {
        return ViewModel(player).VMName;
    }
    public static unsafe void SetViewModel(CCSPlayerController player, string model)
    {
        ViewModel(player).SetModel(model);
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
}