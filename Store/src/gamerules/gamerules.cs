using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using static Store.Store;

namespace Store;

public static class GameRules
{
    public static CCSGameRules GlobalGameRules { get; set; } = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;

    public static bool IgnoreWarmUp()
    {
        return Convert.ToBoolean(Instance.Config.Credits["ignore_warmup"]) && GlobalGameRules.WarmupPeriod;
    }

    public static bool IsPistolRound()
    {
        var halftime = ConVar.Find("mp_halftime")!.GetPrimitiveValue<bool>();
        var maxrounds = ConVar.Find("mp_maxrounds")!.GetPrimitiveValue<int>();

        return GlobalGameRules.TotalRoundsPlayed == 0 ||
               (halftime && maxrounds / 2 == GlobalGameRules.TotalRoundsPlayed) ||
               GlobalGameRules.GameRestart;
    }
}
