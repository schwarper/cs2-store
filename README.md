# cs2-store
A store plugin designed to enhance your gameplay by providing a dynamic credit system that allows players to purchase essential items directly from the store.  

# 🔔 Notice
Some companies modify this plugin, remove the original author name, and claim it as their own. This is unethical and against open-source principles.
Please use the official version here and contribute via pull requests if you wish to help improve it.

- [Installation](https://github.com/schwarper/cs2-store?tab=readme-ov-file#-installation)
- [Installation (Video)](https://github.com/schwarper/cs2-store?tab=readme-ov-file#-installation-video)
- [Modules](https://github.com/schwarper/cs2-store?tab=readme-ov-file#-modules)
- [Api Example](https://github.com/schwarper/cs2-store?tab=readme-ov-file#%EF%B8%8F-api-example)
- [Menu Style](https://github.com/schwarper/cs2-store?tab=readme-ov-file#-menu-style)
---

## 📦 Installation  

### 1) Prerequisites  
- This plugin requires the following dependency:  
  ➡ **[CS2MenuManager](https://github.com/schwarper/CS2MenuManager)**  
  ```Make sure to install this dependency before proceeding.```  

### 2) Download the Plugin  
- Download the latest release from:  
  **[https://github.com/schwarper/cs2-store/releases](https://github.com/schwarper/cs2-store/releases)**  

### 3) Configure Plugin Settings  
After installation:  
- Rename the configuration files in:  
  `addons/counterstrikesharp/configs/plugins/cs2-store/`  
  to the following:  
  - `cs2-store.json` → Define available store items here.  
  - `config.toml` → Configure plugin and database settings.  

### 4) Load/Reload the Plugin  
To activate:  
- Restart your server  
**OR**  
- Run these commands in the server console:  
  ```css_plugins load cs2-store``` (Load the plugin)  
  ```css_plugins reload Store``` (Reload after changes)  

### Custom weapon subclasses

Custom weapons use AnimGraph2 VData subclasses:

```json
{
  "type": "customweapon",
  "weapon": "weapon_ak47:weapon_ak47+1550"
}
```

- `weapon` is `base weapon:custom VData subclass`.
- The standard numeric item definition is restored automatically on unequip (`7` for AK-47, `42`/`59` for the team knife, and so on).
- The target model and VData must be authored for the current AnimGraph2 first-person skeleton. A legacy or mismatched model can still produce broken hands or duplicate gloves even when the plugin state is correct.

#### 🎥 Installation Video  
Watch the step-by-step guide:  
[Installation Guide](https://files.catbox.moe/uzadjw.mp4)


---

## 🎲 Modules
You can find the offical modules:  
[CS2 Store Modules](https://github.com/schwarper/cs2-store-modules)

## 🕸️ Api Example
- Give player an item or credits
```csharp
public class TestModule : BasePlugin
{
    public override string ModuleName => "Test Module";
    public override string ModuleVersion => "0.0.1";

    private IStoreApi? _storeApi;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _storeApi = IStoreApi.Capability.Get();
    }

    public void GivePlayerCredits(CCSPlayerController player, int credits)
    {
        _storeApi?.GivePlayerCredits(player, credits);
    }

    public bool GivePlayerItem(CCSPlayerController player, string uniqueId)
    {
        // Checks if item is exist.
        if (_storeApi?.GetItem(uniqueId) is not { } item)
            return false;

        // Checks if player has already this item
        if (_storeApi.Item_PlayerHas(player, item["type"], uniqueId, ignoreVip: false))
            return false;

        // Give item
        _storeApi.Item_Give(player, item);
        return true;
    }
}
```

- Add module
```csharp
//Module type
[StoreItemType("test")]
//We use IItemModule
public class TestModule : BasePlugin, IItemModule
{
    public override string ModuleName => "Test Module";
    public override string ModuleVersion => "0.0.1";

    // You can also use this way.
    // However, we won't use _storeApi anywhere for this example.
    //private IStoreApi? _storeApi;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        // You need to use assembly here.
        IStoreApi.Capability.Get()?.RegisterModules(Assembly.GetExecutingAssembly());
        //_storeApi = IStoreApi.Capability.Get();
        //_storeApi?.RegisterModules(Test.Assembly);
    }

    // ============================
    // IItemModule dependencies
    // ============================

    // Sets if item is equipable
    public bool Equipable => false;

    // Sets if player has to be alive/dead to purchase
    // True => alive only
    // False => dead only
    // Null => everyone can purchase
    public bool? RequiresAlive => null;

    // You can use this execute as OnAllPluginsLoaded.
    public void OnPluginStart() { Console.WriteLine($"Test OnPluginStart"); }
    public void OnMapStart() { }
    public void OnServerPrecacheResources(ResourceManifest manifest) { }
    
    // If you set false, they cannot equip / unequip, if true, they can.
    public bool OnEquip(CCSPlayerController player, Dictionary<string, string> item)
    {
        return true;
    }
    public bool OnUnequip(CCSPlayerController player, Dictionary<string, string> item, bool update)
    {
        return true;
    }
}
```
## 🔖 Menu Style
**CenterHtmlMenu**

![image](https://files.catbox.moe/gz8x5e.png)

**ChatMenu**

![image](https://files.catbox.moe/85ix04.png)

**ConsoleMenu**

![image](https://files.catbox.moe/m47qri.png)

**WasdMenu**

![image](https://files.catbox.moe/kogxzp.png)
