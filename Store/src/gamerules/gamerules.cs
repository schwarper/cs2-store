using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static Store.Store;

namespace Store;

public static class GameRules
{
    public static CCSGameRules GlobalGameRules { get; set; } = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;

    public static bool IgnoreWarmUp()
    {
        return Convert.ToBoolean(Instance.Config.Credits["ignore_warmup"]) && GlobalGameRules.WarmupPeriod;
    }
}