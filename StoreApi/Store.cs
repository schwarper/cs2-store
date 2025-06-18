using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace StoreApi;

public abstract class Store
{
    public class StorePlayer
    {
        public required ulong SteamId { get; set; }
        public required string PlayerName { get; set; }
        public int Credits { get; set; }
        public int OriginalCredits { get; set; }
        public DateTime DateOfJoin { get; set; }
        public DateTime DateOfLastJoin { get; set; }
        public bool? BPlayerIsLoaded;
    }

    public class StoreItem
    {
        public required ulong SteamId { get; set; }
        public int Price { get; set; }
        public required string Type { get; set; }
        public required string UniqueId { get; set; }
        public DateTime DateOfPurchase { get; set; }
        public DateTime DateOfExpiration { get; set; }
    }

    public class StoreEquipment
    {
        public required ulong SteamId { get; set; }
        public required string Type { get; set; }
        public required string UniqueId { get; set; }
        public int Slot { get; set; }
    }

    public class StoreItemTypes
    {
        public required string Type;
        public required Action MapStart;
        public required Action<ResourceManifest> ServerPrecacheResources;
        public required Func<CCSPlayerController, Dictionary<string, string>, bool> Equip;
        public required Func<CCSPlayerController, Dictionary<string, string>, bool, bool> Unequip;
        public required bool Equipable;
        public bool? Alive;
    }
    public class PlayerTimer
    {
        public Timer? CreditIntervalTimer { get; set; }
    }

    public interface IItemModule
    {
        void OnPluginStart();
        void OnMapStart();
        void OnServerPrecacheResources(ResourceManifest manifest);
        bool OnEquip(CCSPlayerController player, Dictionary<string, string> item);
        bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update);
        bool Equipable { get; }
        bool? RequiresAlive { get; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class StoreItemTypeAttribute(string name) : Attribute
    {
        public string Name { get; } = name;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class StoreItemTypesAttribute(string[] name) : Attribute
    {
        public string[] Names { get; } = name;
    }
}