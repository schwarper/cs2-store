using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace StoreApi;

public abstract class Store
{
    public class Store_Player
    {
        public required ulong SteamID { get; set; }
        public required string PlayerName { get; set; }
        public int Credits { get; set; }
        public int OriginalCredits { get; set; }
        public DateTime DateOfJoin { get; set; }
        public DateTime DateOfLastJoin { get; set; }
        public bool? bPlayerIsLoaded;
    }

    public class Store_Item
    {
        public required ulong SteamID { get; set; }
        public int Price { get; set; }
        public required string Type { get; set; }
        public required string UniqueId { get; set; }
        public DateTime DateOfPurchase { get; set; }
        public DateTime DateOfExpiration { get; set; }
    }

    public class Store_Equipment
    {
        public required ulong SteamID { get; set; }
        public required string Type { get; set; }
        public required string UniqueId { get; set; }
        public int Slot { get; set; }
    }

    public class Store_Item_Types
    {
        public required string Type;
        public required Action MapStart;
        public required Action<ResourceManifest> ServerPrecacheResources;
        public required Func<CCSPlayerController, Dictionary<string, string>, bool> Equip;
        public required Func<CCSPlayerController, Dictionary<string, string>, bool, bool> Unequip;
        public required bool Equipable;
        public bool? Alive;
    }
    public class Player
    {
        public Timer? CreditIntervalTimer { get; set; }
    }
}