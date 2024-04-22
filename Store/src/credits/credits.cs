using CounterStrikeSharp.API.Core;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Credits
{
    // Get player credits safely
    public static int Get(CCSPlayerController player)
    {
        var storePlayer = Instance.GlobalStorePlayers.FirstOrDefault(p => p.SteamID == player.SteamID);
        if (storePlayer != null)
        {
            return storePlayer.Credits;
        }
        else
        {
            // Handle case where player is not found
            // Could log an error or throw a custom exception if necessary
            return -1;
        }
    }
    public static int GetOriginal(CCSPlayerController player)
    {
        var storePlayer = Instance.GlobalStorePlayers.FirstOrDefault(p => p.SteamID == player.SteamID);
        if (storePlayer != null)
        {
            return storePlayer.OriginalCredits;
        }
        else
        {
            // Handle case where player is not found
            // Could log an error or throw a custom exception if necessary
            return -1;
        }
    }
    public static int SetOriginal(CCSPlayerController player, int credits)
    {
        var storePlayer = Instance.GlobalStorePlayers.FirstOrDefault(p => p.SteamID == player.SteamID);
        if (storePlayer != null)
        {
            storePlayer.OriginalCredits = credits;
            return storePlayer.OriginalCredits;
        }

        return -1;
    }
    // Set player credits safely
    public static int Set(CCSPlayerController player, int credits)
    {
        var storePlayer = Instance.GlobalStorePlayers.FirstOrDefault(p => p.SteamID == player.SteamID);
        if (storePlayer != null)
        {
            storePlayer.Credits = credits;
            return storePlayer.Credits;
        }
        return -1;
    }

    // Give credits to a player safely
    public static void Give(CCSPlayerController player, int credits)
    {
        var storePlayer = Instance.GlobalStorePlayers.FirstOrDefault(p => p.SteamID == player.SteamID);
        if (storePlayer != null)
        {
            storePlayer.Credits = Math.Max(storePlayer.Credits + credits, 0);
        }
        else
        {
            // Handle case where player is not found
            // Could add logging or error handling
        }
    }
}

