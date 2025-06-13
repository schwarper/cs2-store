using System.Text.Json;

namespace Store.Extension;

public static class JsonExtensions
{
    public static bool IsValueKindObject(this JsonValueKind valueKind)
    {
        return valueKind == JsonValueKind.Object;
    }

    public static List<JsonProperty> GetElementJsonProperty(this JsonElement element, List<string> ignorePropNameList)
    {
        return [.. element.EnumerateObject().Where(prop => ignorePropNameList.Contains(prop.Name))];
    }

    public static Dictionary<string, Dictionary<string, string>> ExtractItems(this JsonElement category)
    {
        Dictionary<string, Dictionary<string, string>> itemsDictionary = [];

        foreach (JsonProperty subItem in category.EnumerateObject())
        {
            if (subItem.Value.ValueKind == JsonValueKind.Object)
            {
                if (subItem.Value.TryGetProperty("uniqueid", out JsonElement uniqueIdElement))
                {
                    string uniqueId = uniqueIdElement.GetString() ?? $"unknown_{subItem.Name}";
                    Dictionary<string, string> itemData = subItem.Value.EnumerateObject()
                        .ToDictionary(prop => prop.Name, prop => prop.Value.ToString());

                    itemData["name"] = subItem.Name;
                    itemsDictionary[uniqueId] = itemData;
                }
                else
                {
                    Dictionary<string, Dictionary<string, string>> nestedItems = ExtractItems(subItem.Value);
                    foreach (KeyValuePair<string, Dictionary<string, string>> nestedItem in nestedItems)
                    {
                        itemsDictionary[nestedItem.Key] = nestedItem.Value;
                    }
                }
            }
        }

        return itemsDictionary;
    }
}