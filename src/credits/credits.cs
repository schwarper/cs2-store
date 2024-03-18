using CounterStrikeSharp.API.Core;
using static Store.Store;

namespace Store;

internal static class Credits
{
    internal static int Get(CCSPlayerController player)
    {
        return Instance.GlobalStorePlayers.Single(p => p.SteamID == player.SteamID).Credits;
    }

    internal static void Set(CCSPlayerController player, int credits)
    {
        if (credits < 0)
        {
            credits = 0;
        }

        Instance.GlobalStorePlayers.Single(p => p.SteamID == player.SteamID).Credits = credits;
    }

    internal static void Give(CCSPlayerController player, int credits)
    {
        Instance.GlobalStorePlayers.Single(p => p.SteamID == player.SteamID).Credits += credits;
    }
}