using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;
using TagsApi;
using static Store.Store;
using static TagsApi.Tags;

namespace Store;

public static class Item_Tags
{
    private static ITagApi? _tagApi;
    private static readonly string[] TagTypes = ["chatcolor", "namecolor", "scoretag", "chattag"];
    private static bool _scoreTagExists = false;
    private static bool _othersExists = false;

    public static void OnPluginStart()
    {
        foreach (string tagType in TagTypes)
        {
            Item.RegisterType(tagType, OnMapStart, OnServerPrecacheResources, OnEquip, OnUnequip, true, null);
        }

        _scoreTagExists = Item.IsAnyItemExistInType("scoretag");
        _othersExists = Item.IsAnyItemExistInTypes(["chatcolor", "namecolor", "chattag"]);
    }

    public static void OnPluginEnd()
    {
        if (_tagApi == null)
            return;

        if (_othersExists)
            _tagApi.OnMessageProcessPre -= OnMessageProcess;

        if (_scoreTagExists)
            _tagApi.OnTagsUpdatedPre -= OnTagsUpdatedPre;
    }

    public static void OnPluginsAllLoaded()
    {
        try
        {
            _tagApi = ITagApi.Capability.Get();

            if (_tagApi != null)
            {
                if (_othersExists)
                    _tagApi.OnMessageProcessPre += OnMessageProcess;

                if (_scoreTagExists)
                    _tagApi.OnTagsUpdatedPre += OnTagsUpdatedPre;

                Console.WriteLine("[Store] TagsApi features successfully loaded");
            }
        }
        catch (KeyNotFoundException)
        {
            Console.WriteLine("[Store] TagsApi plugin not found, tag features will be disabled");
            _tagApi = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Store] Error loading TagsApi: {ex.Message}");
            _tagApi = null;
        }
    }

    private static void OnMapStart() { }
    private static void OnServerPrecacheResources(ResourceManifest manifest) { }

    private static bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        if (_tagApi != null && item["type"] == "scoretag")
        {
            string tag = item["value"];
            TagPrePost prePost = item.TryGetValue("pre", out string? p) && p == "true" ? TagPrePost.Pre : TagPrePost.Post;
            _tagApi.AddAttribute(player, TagType.ScoreTag, prePost, tag);
        }
        return true;
    }

    private static bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        if (_tagApi != null && item["type"] == "scoretag")
        {
            string? scoreTag = _tagApi.GetAttribute(player, TagType.ScoreTag);
            string valueToRemove = item["value"];

            if (scoreTag != null && scoreTag.Contains(valueToRemove))
            {
                scoreTag = scoreTag.Replace(valueToRemove, string.Empty);
                _tagApi.SetAttribute(player, TagType.ScoreTag, scoreTag);
            }
        }
        return true;
    }

    private static HookResult OnMessageProcess(MessageProcess mp)
    {
        if (_tagApi == null) return HookResult.Continue;

        if (!Instance.GlobalStorePlayerEquipments.Any(kvp => kvp.Type is "chattag" or "chatcolor" or "namecolor"))
            return HookResult.Continue;

        ProcessChatTags(mp);
        ProcessChatColor(mp);
        ProcessNameColor(mp);

        return HookResult.Continue;
    }

    private static void ProcessChatTags(MessageProcess mp)
    {
        Item.GetPlayerEquipments(mp.Player, "chattag").ForEach(tag =>
        {
            Dictionary<string, string>? item = Item.GetItem(tag.UniqueId);
            if (item == null) return;

            TagPrePost pre = item.TryGetValue("pre", out string? p) && p == "true" ? TagPrePost.Pre : TagPrePost.Post;
            string color = item.TryGetValue("color", out string? c) ? c : "{white}";
            string newTag = color.ReplaceColorTags() + item["value"]!;

            if (pre == TagPrePost.Pre)
                mp.Tag.ChatTag = newTag + mp.Tag.ChatTag;
            else
                mp.Tag.ChatTag += newTag;
        });
    }

    private static void ProcessChatColor(MessageProcess mp)
    {
        StoreApi.Store.Store_Equipment? chatColor = Item.GetPlayerEquipments(mp.Player, "chatcolor").FirstOrDefault();
        if (chatColor == null) return;

        Dictionary<string, string>? item = Item.GetItem(chatColor.UniqueId);
        if (item == null) return;

        mp.Tag.ChatColor = item["value"];
    }

    private static void ProcessNameColor(MessageProcess mp)
    {
        StoreApi.Store.Store_Equipment? nameColor = Item.GetPlayerEquipments(mp.Player, "namecolor").FirstOrDefault();
        if (nameColor == null) return;

        Dictionary<string, string>? item = Item.GetItem(nameColor.UniqueId);
        if (item == null) return;

        mp.Tag.NameColor = item["value"];
    }

    private static void OnTagsUpdatedPre(CCSPlayerController player, Tag tag)
    {
        if (_tagApi == null) return;

        StoreApi.Store.Store_Equipment? scoreTag = Item.GetPlayerEquipments(player, "scoretag").FirstOrDefault();
        if (scoreTag == null) return;

        Dictionary<string, string>? item = Item.GetItem(scoreTag.UniqueId);
        if (item == null) return;

        if (_tagApi.GetAttribute(player, TagType.ScoreTag)?.Contains(item["value"]) == true)
            return;

        TagPrePost prePost = item.TryGetValue("pre", out string? p) && p == "true" ? TagPrePost.Pre : TagPrePost.Post;
        string newTag = item["value"]!;
        _tagApi.AddAttribute(player, TagType.ScoreTag, prePost, newTag);
    }
}
