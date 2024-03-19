using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static CounterStrikeSharp.API.Core.Listeners;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    private void Smoke_OnPluginStart()
    {
        new StoreAPI().RegisterType("smoke", Smoke_OnMapStart, Smoke_OnEquip, Smoke_OnUnequip, true, true);

        RegisterListener<OnEntitySpawned>((CEntityInstance entity) =>
        {
            ChangeSmokeColor(entity);
        });
    }
    private void Smoke_OnMapStart()
    {
    }
    private bool Smoke_OnEquip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
    private bool Smoke_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }

    private void ChangeSmokeColor(CEntityInstance entity)
    {
        if (entity.DesignerName != "smokegrenade_projectile")
        {
            return;
        }

        CSmokeGrenadeProjectile smokeGrenadeEntity = new(entity.Handle);

        if (smokeGrenadeEntity.Handle == IntPtr.Zero)
        {
            return;
        }

        Server.NextFrame(() =>
        {
            CCSPlayerPawn? thrower = smokeGrenadeEntity.Thrower.Value;

            if (thrower == null)
            {
                return;
            }

            CBasePlayerController? player = thrower.Controller.Value;

            if (player == null)
            {
                return;
            }

            Store_PlayerItem? item = GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "smoke");

            if (item == null)
            {
                return;
            }

            if (item.UniqueId == "colorsmoke")
            {
                smokeGrenadeEntity.SmokeColor.X = Random.Shared.NextSingle() * 255.0f;
                smokeGrenadeEntity.SmokeColor.Y = Random.Shared.NextSingle() * 255.0f;
                smokeGrenadeEntity.SmokeColor.Z = Random.Shared.NextSingle() * 255.0f;
            }
            else
            {
                string[] colorValues = item.Color.Split(' ');

                smokeGrenadeEntity.SmokeColor.X = float.Parse(colorValues[0]);
                smokeGrenadeEntity.SmokeColor.Y = float.Parse(colorValues[1]);
                smokeGrenadeEntity.SmokeColor.Z = float.Parse(colorValues[2]);
            }
        });
    }
}