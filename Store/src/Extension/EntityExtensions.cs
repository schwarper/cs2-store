using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static Store.Store;

namespace Store.Extension;

public static class EntityExtensions
{
    public static CParticleSystem? CreateFollowingParticle(this CBaseEntity ent, string effectName, string? acceptInput)
    {
        if (ent.AbsOrigin is not { } origin)
            return null;
        
        CParticleSystem? entity = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
        if (entity?.IsValid is not true)
            return null;

        entity.EffectName = effectName;
        entity.DispatchSpawn();
        entity.Teleport(origin);
        entity.AcceptInput("FollowEntity", ent, entity, "!activator");
        
        if (!string.IsNullOrEmpty(acceptInput))
            entity.AcceptInput(acceptInput);

        return entity;
    }
    
    public static CBeam? CreateFollowingBeam(this CBaseEntity ent, float width, string color, Vector? endPos)
    {
        if (ent.AbsOrigin is not { } origin)
            return null;

        CBeam? entity = Utilities.CreateEntityByName<CBeam>("env_beam");
        if (entity?.IsValid is not true)
            return null;

        entity.RenderMode = RenderMode_t.kRenderTransColor;
        entity.Width = width;
        entity.Render = color == "random" ? GetRandomColor() : Color.FromName(color);
        
        entity.Teleport(origin);
        entity.AcceptInput("FollowEntity", ent, entity, "!activator");

        endPos ??= origin;
        entity.EndPos.X = endPos.X;
        entity.EndPos.Y = endPos.Y;
        entity.EndPos.Z = endPos.Z;
        Utilities.SetStateChanged(entity, "CBeam", "m_vecEndPos");

        return entity;
    }
    
    public static CParticleSystem? CreateFollowingParticle(this CCSPlayerController player, string effectName, string? acceptInput)
    {
        if (player.PlayerPawn.Value is not { AbsOrigin: { } origin } playerPawn)
            return null;
        
        CParticleSystem? entity = Utilities.CreateEntityByName<CParticleSystem>("info_particle_system");
        if (entity?.IsValid is not true)
            return null;

        entity.EffectName = effectName;
        entity.DispatchSpawn();
        entity.Teleport(origin);
        entity.AcceptInput("FollowEntity", playerPawn, playerPawn, "!activator");
        
        if (!string.IsNullOrEmpty(acceptInput))
            entity.AcceptInput(acceptInput);

        return entity;
    }
    
    public static CBeam? CreateFollowingBeam(this CCSPlayerController player, float width, string color, Vector? endPos, bool fromEyePosition)
    {
        if (player.PlayerPawn.Value is not { AbsOrigin: { } origin })
            return null;

        CBeam? entity = Utilities.CreateEntityByName<CBeam>("env_beam");
        if (entity?.IsValid is not true)
            return null;

        entity.RenderMode = RenderMode_t.kRenderTransColor;
        entity.Width = width;
        entity.Render = color == "random" ? GetRandomColor() : Color.FromName(color);
        
        entity.Teleport(fromEyePosition ? VectorExtensions.GetEyePosition(player) : origin);
        entity.AcceptInput("FollowEntity", player, entity, "!activator");

        endPos ??= origin;
        entity.EndPos.X = endPos.X;
        entity.EndPos.Y = endPos.Y;
        entity.EndPos.Z = endPos.Z;
        Utilities.SetStateChanged(entity, "CBeam", "m_vecEndPos");

        return entity;
    }
    
    private static Color GetRandomColor()
    {
        var randomColorName = (KnownColor?)Enum.GetValues(typeof(KnownColor)).GetValue(Instance.Random.Next(Enum.GetValues(typeof(KnownColor)).Length));
        return randomColorName.HasValue ? Color.FromKnownColor(randomColorName.Value) : Color.Green;
    }
}