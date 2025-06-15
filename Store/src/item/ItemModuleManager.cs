using System.Reflection;
using static StoreApi.Store;

namespace Store;

public static class ItemModuleManager
{
    public static readonly Dictionary<string, IItemModule> Modules = [];

    public static void RegisterModules(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IItemModule).IsAssignableFrom(t)))
        {
            if (Activator.CreateInstance(type) is not IItemModule module)
                continue;

            if (type.GetCustomAttribute<StoreItemTypeAttribute>() is { } attr)
            {
                LoadModule(attr.Name, module);
            }
            else if (type.GetCustomAttribute<StoreItemTypesAttribute>()?.Names is { } attrs)
            {
                foreach (string attrName in attrs)
                {
                    LoadModule(attrName, module);
                }
            }
        }
    }

    private static void LoadModule(string name, IItemModule module)
    {
        Modules[name] = module;
        Console.WriteLine($"[CS2-Store] Module '{name}' has been added.");
        module.OnPluginStart();
    }
}