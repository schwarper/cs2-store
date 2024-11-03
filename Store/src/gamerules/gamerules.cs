using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using static Store.Config_Config;

namespace Store;

public static class GameRules
{
    private static CCSGameRulesProxy? GameRulesProxy;
    private static readonly ConVar mp_halftime = ConVar.Find("mp_halftime")!;
    private static readonly ConVar mp_maxrounds = ConVar.Find("mp_maxrounds")!;

    public static bool IgnoreWarmUp()
    {
        if (GameRulesProxy?.IsValid is not true)
        {
            GameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
        }

        return Config.Credits.IgnoreWarmup && (GameRulesProxy?.GameRules?.WarmupPeriod ?? false);
    }

    public static bool IsPistolRound()
    {
        if (GameRulesProxy?.IsValid is not true)
        {
            GameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
        }

        bool halftime = mp_halftime.GetPrimitiveValue<bool>();
        int maxrounds = mp_maxrounds.GetPrimitiveValue<int>();

        return GameRulesProxy?.GameRules?.TotalRoundsPlayed == 0 ||
               (halftime && maxrounds / 2 == GameRulesProxy?.GameRules?.TotalRoundsPlayed) ||
               (GameRulesProxy?.GameRules?.GameRestart ?? false);
    }
}
