using System.Reflection;
using static StoreApi.Store;

namespace Store;

public static class ItemModuleManager
{
    public static readonly Dictionary<string, IItemModule> Modules = new();

    public static void RegisterModules(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IItemModule).IsAssignableFrom(t)))
        {
            if (Activator.CreateInstance(type) is not IItemModule module)
                continue;

            if (type.GetCustomAttribute<StoreItemTypeAttribute>() is { } attr)
            {
                Modules[attr.Name] = module;
            }
            else if (type.GetCustomAttribute<StoreItemTypesAttribute>() is { } attrs)
            {
                foreach (string attrName in attrs.Names)
                {
                    Modules[attrName] = module;
                }
            }
        }
    }
}