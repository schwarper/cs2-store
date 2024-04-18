using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using static StoreApi.Store;

namespace Store;

public partial class Store
{
    public StoreConfig Config { get; set; } = new StoreConfig();
    public List<Store_Player> GlobalStorePlayers { get; set; } = [];
    public List<Store_Item> GlobalStorePlayerItems { get; set; } = [];
    public List<Store_Equipment> GlobalStorePlayerEquipments { get; set; } = [];
    public List<Store_Item_Types> GlobalStoreItemTypes { get; set; } = [];
    public Dictionary<CCSPlayerController, Player> GlobalDictionaryPlayer { get; set; } = [];
    public int GlobalTickrate { get; set; } = 0;
    public static Store Instance { get; set; } = new();

    public readonly Random random = new();
}