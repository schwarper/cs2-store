using CounterStrikeSharp.API.Core;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Credits
{
    public static int Get(CCSPlayerController player)
    {
        return Instance.GlobalStorePlayers.Single(p => p.SteamID == player.SteamID).Credits;
    }

    public static void Set(CCSPlayerController player, int credits)
    {
        Instance.GlobalStorePlayers.Single(p => p.SteamID == player.SteamID).Credits = Math.Max(credits, 0);
    }

    public static void Give(CCSPlayerController player, int credits)
    {
        Store_Player storePlayer = Instance.GlobalStorePlayers.Single(p => p.SteamID == player.SteamID);

        storePlayer.Credits = Math.Max(storePlayer.Credits + credits, 0);
    }
}
