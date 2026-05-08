using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using static Store.Config_Config;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Credits
{
    public sealed class GameplayCapStatus
    {
        public bool Enabled { get; set; }
        public int MaxCredits { get; set; }
        public int EarnedCredits { get; set; }
        public int RemainingCredits { get; set; }
        public TimeSpan TimeUntilReset { get; set; }
    }

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

    public static int GiveGameplay(CCSPlayerController player, int credits)
    {
        Store_Player? storePlayer = GetStorePlayer(player);
        if (storePlayer == null) return -1;

        if (credits <= 0)
        {
            return Give(player, credits);
        }

        Config_DailyEarnedCreditsCap capConfig = Config.DailyEarnedCreditsCap;
        if (!capConfig.Enabled || capConfig.MaxCreditsPerDay <= 0)
        {
            Give(player, credits);
            return credits;
        }

        int resetHours = capConfig.ResetEveryHours > 0 ? capConfig.ResetEveryHours : 24;
        DateTime now = DateTime.UtcNow;

        EnsureGameplayCapWindow(storePlayer, capConfig, now, resetHours);

        int previousEarned = storePlayer.DailyGameplayCreditsEarned;
        int remaining = capConfig.MaxCreditsPerDay - storePlayer.DailyGameplayCreditsEarned;
        if (remaining <= 0)
        {
            return 0;
        }

        int granted = Math.Min(credits, remaining);
        Give(player, granted);
        storePlayer.DailyGameplayCreditsEarned += granted;

        if (capConfig.NotifyOnceOnReached &&
            granted > 0 &&
            previousEarned < capConfig.MaxCreditsPerDay &&
            storePlayer.DailyGameplayCreditsEarned >= capConfig.MaxCreditsPerDay &&
            (!capConfig.UseLocalizedMessages || !string.IsNullOrWhiteSpace(capConfig.ReachedCapMessage)))
        {
            string template = capConfig.UseLocalizedMessages
                ? Instance.Localizer.ForPlayer(player, "cap_reached")
                : capConfig.ReachedCapMessage;

            player.PrintToChat($"{Config.Settings.Tag}{FormatCapMessage(template, storePlayer.DailyGameplayCreditsEarned, capConfig.MaxCreditsPerDay)}");
        }

        return granted;
    }

    public static GameplayCapStatus GetGameplayCapStatus(CCSPlayerController player)
    {
        Store_Player? storePlayer = GetStorePlayer(player);
        Config_DailyEarnedCreditsCap capConfig = Config.DailyEarnedCreditsCap;

        if (storePlayer == null)
        {
            return new GameplayCapStatus
            {
                Enabled = capConfig.Enabled && capConfig.MaxCreditsPerDay > 0,
                MaxCredits = capConfig.MaxCreditsPerDay,
                EarnedCredits = 0,
                RemainingCredits = capConfig.MaxCreditsPerDay,
                TimeUntilReset = TimeSpan.FromHours(capConfig.ResetEveryHours > 0 ? capConfig.ResetEveryHours : 24)
            };
        }

        int resetHours = capConfig.ResetEveryHours > 0 ? capConfig.ResetEveryHours : 24;
        DateTime now = DateTime.UtcNow;
        EnsureGameplayCapWindow(storePlayer, capConfig, now, resetHours);

        DateTime? windowStart = storePlayer.DailyGameplayCreditsWindowStart;
        TimeSpan timeUntilReset = windowStart.HasValue
            ? ((windowStart.Value + TimeSpan.FromHours(resetHours)) - now)
            : TimeSpan.FromHours(resetHours);

        if (timeUntilReset < TimeSpan.Zero)
        {
            timeUntilReset = TimeSpan.Zero;
        }

        int maxCredits = Math.Max(capConfig.MaxCreditsPerDay, 0);
        int earned = Math.Max(storePlayer.DailyGameplayCreditsEarned, 0);
        int remaining = Math.Max(maxCredits - earned, 0);

        return new GameplayCapStatus
        {
            Enabled = capConfig.Enabled && maxCredits > 0,
            MaxCredits = maxCredits,
            EarnedCredits = earned,
            RemainingCredits = remaining,
            TimeUntilReset = timeUntilReset
        };
    }

    public static void ResetGameplayCap(Store_Player storePlayer)
    {
        storePlayer.DailyGameplayCreditsEarned = 0;
        storePlayer.DailyGameplayCreditsWindowStart = null;
    }

    private static void EnsureGameplayCapWindow(Store_Player storePlayer, Config_DailyEarnedCreditsCap capConfig, DateTime now, int resetHours)
    {
        if (!capConfig.Enabled || capConfig.MaxCreditsPerDay <= 0)
            return;

        if (storePlayer.DailyGameplayCreditsWindowStart is not DateTime windowStart ||
            now - windowStart >= TimeSpan.FromHours(resetHours))
        {
            storePlayer.DailyGameplayCreditsWindowStart = now;
            storePlayer.DailyGameplayCreditsEarned = 0;
        }
    }

    private static string FormatCapMessage(string template, params object[] args)
    {
        try
        {
            return string.Format(template, args);
        }
        catch
        {
            return template;
        }
    }
}
