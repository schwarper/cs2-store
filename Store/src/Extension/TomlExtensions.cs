using System.Reflection;
using Tomlyn.Model;

namespace Store.Extension;

public static class TomlExtensions
{
    public static T? GetSection<T>(this TomlTable model, string sectionName) where T : new()
    {
        if (!model.TryGetValue(sectionName, out var sectionObj) || sectionObj is not TomlTable section)
            return default;

        return MapTomlTableToObject<T>(section);
    }

    public static T MapTomlTableToObject<T>(this TomlTable table) where T : new()
    {
        var obj = new T();
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props)
        {
            if (!prop.CanWrite || !table.TryGetValue(prop.Name, out var value))
                continue;

            try
            {
                if (prop.PropertyType == typeof(List<string>) && value is TomlArray array)
                    prop.SetValue(obj, array.OfType<string>().ToList());
                else
                    prop.SetValue(obj, Convert.ChangeType(value, prop.PropertyType));
            }
            catch
            { }
        }

        return obj;
    }
}