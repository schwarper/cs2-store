using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using static Store.Config_Config;

namespace Store;

public static class GameRules
{
    public static CCSGameRules GlobalGameRules { get; set; } = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;

    public static bool IgnoreWarmUp()
    {
        return Config.Credits.IgnoreWarmup && GlobalGameRules.WarmupPeriod;
    }

    public static bool IsPistolRound()
    {
        bool halftime = ConVar.Find("mp_halftime")!.GetPrimitiveValue<bool>();
        int maxrounds = ConVar.Find("mp_maxrounds")!.GetPrimitiveValue<int>();

        return GlobalGameRules.TotalRoundsPlayed == 0 ||
               (halftime && maxrounds / 2 == GlobalGameRules.TotalRoundsPlayed) ||
               GlobalGameRules.GameRestart;
    }
}
