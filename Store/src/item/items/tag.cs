using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using TagsApi;
using static Store.Store;
using static StoreApi.Store;

namespace Store;

public static class Item_Tag
{
    private static ITagApi? tagApi;
    private static readonly List<string> typeList = ["scoretag", "chattag", "chatcolor", "namecolor"];
    private static PluginCapability<ITagApi> Capability { get; } = new("tags:api");

    public static void OnAllPluginsLoaded()
    {
        tagApi = Capability.Get();

        if (tagApi == null)
        {
            return;
        }

        Item.RegisterType("scoretag", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);
        Item.RegisterType("chattag", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);
        Item.RegisterType("chatcolor", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);
        Item.RegisterType("namecolor", OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);

        Instance.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
    }
    public static void OnMapStart()
    {
    }
    public static void OnServerPrecacheResources(ResourceManifest manifest)
    {
    }
    public static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        switch (item["type"])
        {
            case "scoretag":
                {
                    tagApi?.SetPlayerTag(player, Tags.Tags_Tags.ScoreTag, item["scoretag"]);
                    break;
                }
            case "chattag":
                {
                    tagApi?.SetPlayerTag(player, Tags.Tags_Tags.ChatTag, item["chattag"]);
                    break;
                }
            case "chatcolor":
                {
                    tagApi?.SetPlayerColor(player, Tags.Tags_Colors.ChatColor, item["chatcolor"]);
                    break;
                }
            case "namecolor":
                {
                    tagApi?.SetPlayerColor(player, Tags.Tags_Colors.NameColor, item["namecolor"]);
                    break;
                }
        }

        return true;
    }
    public static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item)
    {
        switch (item["type"])
        {
            case "scoretag":
                {
                    tagApi?.ResetPlayerTag(player, Tags.Tags_Tags.ScoreTag);
                    break;
                }
            case "chattag":
                {
                    tagApi?.ResetPlayerTag(player, Tags.Tags_Tags.ChatTag);
                    break;
                }
            case "chatcolor":
                {
                    tagApi?.ResetPlayerColor(player, Tags.Tags_Colors.ChatColor);
                    break;
                }
            case "namecolor":
                {
                    tagApi?.ResetPlayerColor(player, Tags.Tags_Colors.NameColor);
                    break;
                }
        }
        return true;
    }

    public static HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null)
        {
            return HookResult.Continue;
        }

        Instance.AddTimer(0.1f, () =>
        {
            foreach (string type in typeList)
            {
                SetTags(player, type);
            }
        });

        return HookResult.Continue;
    }

    private static void SetTags(CCSPlayerController player, string type)
    {
        Store_Equipment? item = Instance.GlobalStorePlayerEquipments.FirstOrDefault(p => p.SteamID == player.SteamID && p.Type == type);

        if (item == null)
        {
            return;
        }

        Dictionary<string, string>? itemdata = Item.GetItem(item.Type, item.UniqueId);

        if (itemdata == null)
        {
            return;
        }

        switch (item.Type)
        {
            case "scoretag":
                {
                    tagApi?.SetPlayerTag(player, Tags.Tags_Tags.ScoreTag, itemdata["scoretag"]);
                    break;
                }
            case "chattag":
                {
                    tagApi?.SetPlayerTag(player, Tags.Tags_Tags.ChatTag, itemdata["chattag"]);
                    break;
                }
            case "chatcolor":
                {
                    tagApi?.SetPlayerColor(player, Tags.Tags_Colors.ChatColor, itemdata["chatcolor"]);
                    break;
                }
            case "namecolor":
                {
                    tagApi?.SetPlayerColor(player, Tags.Tags_Colors.NameColor, itemdata["namecolor"]);
                    break;
                }
        }
    }
}