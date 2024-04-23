using CounterStrikeSharp.API.Core;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Credits
{
    public static int Get(CCSPlayerController player)
    {
        return Instance.GlobalStorePlayers.FirstOrDefault(p => p.SteamID == player.SteamID)?.Credits ?? -1;
    }
    public static int GetOriginal(CCSPlayerController player)
    {
        return Instance.GlobalStorePlayers.FirstOrDefault(p => p.SteamID == player.SteamID)?.OriginalCredits ?? -1;
    }
    public static int SetOriginal(CCSPlayerController player, int credits)
    {
        Store_Player? storePlayer = Instance.GlobalStorePlayers.FirstOrDefault(p => p.SteamID == player.SteamID);

        if (storePlayer != null)
        {
            storePlayer.OriginalCredits = credits;

            return storePlayer.OriginalCredits;
        }

        return -1;
    }
    public static int Set(CCSPlayerController player, int credits)
    {
        Store_Player? storePlayer = Instance.GlobalStorePlayers.FirstOrDefault(p => p.SteamID == player.SteamID);

        if (storePlayer != null)
        {
            storePlayer.Credits = credits;

            return storePlayer.Credits;
        }

        return -1;
    }
    public static int Give(CCSPlayerController player, int credits)
    {
        Store_Player? storePlayer = Instance.GlobalStorePlayers.FirstOrDefault(p => p.SteamID == player.SteamID);

        if (storePlayer != null)
        {
            storePlayer.Credits = Math.Max(storePlayer.Credits + credits, 0);

            return storePlayer.Credits;
        }

        return -1;
    }
}