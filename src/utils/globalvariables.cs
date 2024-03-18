using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands.Targeting;

namespace Store;

public partial class Store : BasePlugin
{
    public StoreConfig Config { get; set; } = new StoreConfig();
    public List<Store_Player> GlobalStorePlayers { get; set; } = new();
    public List<Store_PlayerItem> GlobalStorePlayerItems { get; set; } = new();
    public List<Store_PlayerItem> GlobalStorePlayerEquipments { get; set; } = new();
    public List<Store_Item_Types> GlobalStoreItemTypes { get; set; } = new();
    public Dictionary<CCSPlayerController, Player> GlobalDictionaryPlayer { get; set; } = new();
    public static Store Instance { get; set; } = new();
    public static PluginCapability<IStoreAPI> StoreAPI { get; } = new("store:api");

    public readonly Random random = new();

    private static readonly Dictionary<string, TargetType> TargetTypeMap = new(StringComparer.OrdinalIgnoreCase)
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