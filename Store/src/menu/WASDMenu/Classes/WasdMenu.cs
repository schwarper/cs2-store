using CounterStrikeSharp.API.Core;

namespace Store;

public class WasdMenu : IWasdMenu
{
    public string? Title { get; set; } = string.Empty;
    public LinkedList<IWasdMenuOption>? Options { get; set; } = new();
    public LinkedListNode<IWasdMenuOption>? Prev { get; set; } = null;
    public LinkedListNode<IWasdMenuOption> Add(string display, Action<CCSPlayerController, IWasdMenuOption> onChoice)
    {
        Options ??= new();

        WasdMenuOption newOption = new()
        {
            OptionDisplay = display,
            OnChoose = onChoice,
            Index = Options.Count,
            Parent = this
        };
        return Options.AddLast(newOption);
    }
}