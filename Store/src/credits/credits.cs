using CounterStrikeSharp.API.Core;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Credits
{
    public static Store_Player? GetStorePlayer(CCSPlayerController player)
    {
        return Instance.GlobalStorePlayers.FirstOrDefault(p => p.SteamID == player.SteamID);
    }

    public static int Get(CCSPlayerController player)
    {
        return GetStorePlayer(player)?.Credits ?? -1;
    }

    public static int GetOriginal(CCSPlayerController player)
    {
        return GetStorePlayer(player)?.OriginalCredits ?? -1;
    }

    public static int SetOriginal(CCSPlayerController player, int credits)
    {
        Store_Player? storePlayer = GetStorePlayer(player);
        if (storePlayer == null) return -1;

        storePlayer.OriginalCredits = credits;
        return storePlayer.OriginalCredits;
    }

    public static int Set(CCSPlayerController player, int credits)
    {
        Store_Player? storePlayer = GetStorePlayer(player);
        if (storePlayer == null) return -1;

        storePlayer.Credits = credits;
        return storePlayer.Credits;
    }

    public static int Give(CCSPlayerController player, int credits)
    {
        Store_Player? storePlayer = GetStorePlayer(player);
        if (storePlayer == null) return -1;

        storePlayer.Credits = Math.Max(storePlayer.Credits + credits, 0);
        return storePlayer.Credits;
    }
}