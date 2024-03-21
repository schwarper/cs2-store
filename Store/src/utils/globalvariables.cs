using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public StoreConfig Config { get; set; } = new StoreConfig();
    public List<Store_Player> GlobalStorePlayers { get; set; } = new();
    public List<Store_PlayerItem> GlobalStorePlayerItems { get; set; } = new();
    public List<Store_PlayerItem> GlobalStorePlayerEquipments { get; set; } = new();
    public List<Store_Item_Types> GlobalStoreItemTypes { get; set; } = new();
    public Dictionary<CCSPlayerController, Player> GlobalDictionaryPlayer { get; set; } = new();
    public int GlobalTickrate { get; set; } = 0;
    public static Store Instance { get; set; } = new();

    public readonly Random random = new();

    public static readonly Dictionary<string, TargetType> TargetTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "@all", TargetType.GroupAll },
        { "@bots", TargetType.GroupBots },
        { "@human", TargetType.GroupHumans },
        { "@alive", TargetType.GroupAlive },
        { "@dead", TargetType.GroupDead },
        { "@!me", TargetType.GroupNotMe },
        { "@me", TargetType.PlayerMe },
        { "@ct", TargetType.TeamCt },
        { "@t", TargetType.TeamT },
        { "@spec", TargetType.TeamSpec }
    };
}