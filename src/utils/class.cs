using CounterStrikeSharp.API.Core;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Store;

public partial class Store : BasePlugin
{
    public class Store_Player
    {
        public required ulong SteamID { get; set; }
        public required string PlayerName { get; set; }
        public required int Credits { get; set; }
        public DateTime DateOfJoin { get; set; }
        public DateTime DateOfLastJoin { get; set; }
    }
    public class Store_PlayerItem
    {
        public required ulong SteamID { get; set; }
        public required string Type { get; set; }
        public required int Price { get; set; }
        public string UniqueId { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;
        public int Slot { get; set; }
        public DateTime DateOfPurchase { get; set; }
    }
    public class Store_Item
    {
        public required string Name { get; set; }
        public required string Type { get; set; }
        public required int Price { get; set; }
        public string UniqueId { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;
        public int Slot { get; set; }
        public bool Enable { get; set; }
    }
    public class Store_Item_Types
    {
        public required string Type;
        public required Action MapStart;
        public required Func<CCSPlayerController, Store_Item, bool> Equip;
        public required Func<CCSPlayerController, Store_Item, bool> Unequip;
        public required bool Equipable;
        public bool? Alive;
    }
    public class Player
    {
        public Timer? CreditIntervalTimer { get; set; }
    }
}