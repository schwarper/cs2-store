using CounterStrikeSharp.API.Core;
using static Store.Store;

namespace Store;

public static class Credits
{
    public static int Get(CCSPlayerController player)
    {
        return Instance.GlobalStorePlayers.Single(p => p.SteamID == player.SteamID).Credits;
    }

    public static void Set(CCSPlayerController player, int credits)
    {
        if (credits < 0)
        {
            credits = 0;
        }

        Instance.GlobalStorePlayers.Single(p => p.SteamID == player.SteamID).Credits = credits;
    }

    public static void Give(CCSPlayerController player, int credits)
    {
        int playercredits = Get(player);

        if (playercredits + credits < 0)
        {
            Instance.GlobalStorePlayers.Single(p => p.SteamID == player.SteamID).Credits = 0;
        }
        else
        {
            Instance.GlobalStorePlayers.Single(p => p.SteamID == player.SteamID).Credits += credits;
        }
    }
}