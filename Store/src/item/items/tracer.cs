namespace Store;

public partial class Store
{
    public static void Tracer_OnPluginStart()
    {
    }
    /*
    public static void Tracer_OnPluginStart()
    {
        new StoreAPI().RegisterType("tracer", Tracer_OnMapStart, Tracer_OnEquip, Tracer_OnUnequip, true, null);

        Instance.RegisterEventHandler<EventBulletImpact>((@event, info) =>
        {
            var player = @event.Userid;

            if (player == null || !player.IsValid)
            {
                return HookResult.Continue;
            }

            Store_PlayerItem? playertracer = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == "tracer");

            if (playertracer == null)
            {
                return HookResult.Continue;
            }

            CBeam? entity = Utilities.CreateEntityByName<CBeam>("beam");

            if (entity == null || !entity.IsValid)
            {
                return HookResult.Continue;
            }

            entity.SetModel(playertracer.UniqueId);
            entity.DispatchSpawn();
            entity.AcceptInput("Start");

            Vector absorigin = player.PlayerPawn.Value!.AbsOrigin!;
            CNetworkViewOffsetVector offset = player.PlayerPawn.Value.ViewOffset;

            /*
             * TO DO
             * position is not correct.

            Vector position = new(absorigin.X + offset.X, absorigin.Y + offset.Y, absorigin.Z + offset.Z);

            entity.Teleport(position, new QAngle(), new Vector());

            entity.EndPos.X = @event.X;
            entity.EndPos.Y = @event.Y;
            entity.EndPos.Z = @event.Z;

            Utilities.SetStateChanged(entity, "CBeam", "m_vecEndPos");

            Instance.AddTimer(10.0f, () =>
            {
                if (entity != null && entity.IsValid)
                {
                    entity.Remove();
                }
            });

            return HookResult.Continue;
        });
    }
    public static void Tracer_OnMapStart()
    {
        IEnumerable<string> playerTracers = Instance.Config.Items
        .SelectMany(wk => wk.Value)
        .Where(kvp => kvp.Value.Type == "trail")
        .Select(kvp => kvp.Value.UniqueId);

        Instance.RegisterListener<OnServerPrecacheResources>((manifest) =>
        {
            foreach (string UniqueId in playerTracers)
            {
                manifest.AddResource(UniqueId);
            }
        });
    }
    public static bool Tracer_OnEquip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
    public static bool Tracer_OnUnequip(CCSPlayerController player, Store_Item item)
    {
        return true;
    }
    */
}