using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Store;

public static class Vec
{
    public static Vector GetEyePosition(CCSPlayerController player)
    {
        Vector absorigin = player.PlayerPawn.Value!.AbsOrigin!;
        CPlayer_CameraServices camera = player.PlayerPawn.Value!.CameraServices!;

        return new Vector(absorigin.X, absorigin.Y, absorigin.Z + camera.OldPlayerViewOffsetZ);
    }

    public static float CalculateDistance(Vector vector1, Vector vector2)
    {
        float dx = vector2.X - vector1.X;
        float dy = vector2.Y - vector1.Y;
        float dz = vector2.Z - vector1.Z;

        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static void Copy(Vector vector1, Vector vector2)
    {
        vector2.X = vector1.X;
        vector2.Y = vector1.Y;
        vector2.Z = vector1.Z;
    }

    public static bool IsZero(Vector vector)
    {
        return vector.LengthSqr() == 0;
    }
}